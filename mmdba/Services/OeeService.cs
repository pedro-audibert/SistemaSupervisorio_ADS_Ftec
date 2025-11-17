using Microsoft.EntityFrameworkCore;
using mmdba.Data;
using mmdba.Models;
using mmdba.Models.Entidades;
using Microsoft.Extensions.Logging; // Necessário para ILogger
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mmdba.Services
{
    public class OeeService : IOeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeZoneInfo _tz; // Fuso horário
        private readonly ILogger<OeeService> _logger; // Para depuração

        public OeeService(ApplicationDbContext context, ILogger<OeeService> logger)
        {
            _context = context;
            _logger = logger; // Injeção de dependência do Logger

            // Define o fuso horário (o mesmo usado nos relatórios PDF/CSV)
            try
            {
                // Padrão do Windows
                _tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
            catch
            {
                // Padrão do Linux/macOS
                _tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            }
        }

        public async Task<AnaliseOEEViewModel> CalcularOEEAsync(
            string maquinaId,
            DateTime dataInicio,
            DateTime dataFim,
            int? turnoId)
        {
            // 1. PREPARAÇÃO DO MODELO E DATAS
            var dataFimAjustada = dataFim.Date.AddDays(1).AddSeconds(-1);

            // Converte as datas locais do filtro para UTC (o formato do banco)
            var dataInicioUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Unspecified), _tz);
            var dataFimAjustadaUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dataFimAjustada, DateTimeKind.Unspecified), _tz);

            var viewModel = new AnaliseOEEViewModel
            {
                MaquinaId = maquinaId,
                NomeMaquina = "Rotuladora (Exemplo)",
                DataInicio = dataInicio.Date,
                DataFim = dataFimAjustada,
                PerdasNaoPlanejadas = new List<PerdaDetalheViewModel>()
                // ... (propriedades de OEE zeradas por defeito) ...
            };

            // 2. CARREGAR PARÂMETROS DE OEE
            var parametros = await _context.OeeParametrosMaquina
                .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

            if (parametros != null)
            {
                viewModel.VelocidadeIdealPorHora = parametros.VelocidadeIdealPorHora;
                viewModel.HabilitarRefugoManual = parametros.HabilitarRefugoManual;
                viewModel.TaxaAtualizacaoMinutos = parametros.TaxaAtualizacaoMinutos;
            }
            else
            {
                viewModel.TaxaAtualizacaoMinutos = 10; // Default
            }

            // 3. BUSCAR DADOS DE BASE (Uma única vez)

            // Busca os turnos filtrados
            IQueryable<Turno> queryTurnos = _context.Turnos
                .Where(t => t.MaquinaId == maquinaId);

            bool isTurnoEspecifico = turnoId.HasValue && turnoId.Value > 0;

            if (isTurnoEspecifico)
            {
                queryTurnos = queryTurnos.Where(t => t.Id == turnoId.Value);
            }

            var turnosDoPeriodo = await queryTurnos
                .Include(t => t.ParadasPlanejadas)
                .ToListAsync();

            // --- LÓGICA DO "TURNO VIRTUAL" (Passo 7) ---
            if (!turnosDoPeriodo.Any())
            {
                if (isTurnoEspecifico)
                {
                    // O utilizador pediu um turno específico (ex: ID 5), mas ele não existe. Retorna zerado.
                    _logger.LogWarning($"OEE: Turno específico ID {turnoId} não encontrado para a máquina {maquinaId}.");
                    return viewModel;
                }
                else
                {
                    // O utilizador pediu "Todos" (ID 0), mas não há turnos registados.
                    // Criamos um "Turno Virtual" 24/7 para calcular os dados antigos.
                    _logger.LogInformation($"OEE: Nenhum turno encontrado para {maquinaId}. Usando Turno Virtual 24/7.");
                    turnosDoPeriodo.Add(new Turno
                    {
                        Id = 0,
                        Nome = "Período Completo (24/7)",
                        HoraInicio = TimeSpan.Zero, // 00:00:00
                        HoraFim = TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1), // 23:59:59.999...
                        ParadasPlanejadas = new List<ParadaPlanejada>() // Nenhuma parada planejada
                    });
                }
            }

            // Busca todos os eventos relevantes (Alarme/Aviso, ON e OFF) do período de datas
            var eventosDoPeriodo = await _context.EventosMaquina
                .Where(e => e.Origem == maquinaId
                       && e.Timestamp >= dataInicioUtc
                       && e.Timestamp <= dataFimAjustadaUtc
                       && (e.TipoEvento == "Alarme" || e.TipoEvento == "Aviso"))
                .OrderBy(e => e.Timestamp)
                .ToListAsync();

            // Busca o último estado (Alarme/Aviso, ON ou OFF) ANTES do início do filtro
            var estadoAnteriorAlarme = await _context.EventosMaquina
                .Where(e => e.Origem == maquinaId
                       && e.Timestamp < dataInicioUtc
                       && e.TipoEvento == "Alarme")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            var estadoAnteriorAviso = await _context.EventosMaquina
                .Where(e => e.Origem == maquinaId
                       && e.Timestamp < dataInicioUtc
                       && e.TipoEvento == "Aviso")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            // Lista combinada de eventos
            var todosEventos = new List<EventoMaquina>();

            // Adiciona o estado inicial (se for ON) como se tivesse acontecido no início do filtro
            if (estadoAnteriorAlarme != null && estadoAnteriorAlarme.CodigoEvento == "alarmeON")
            {
                todosEventos.Add(new EventoMaquina { Timestamp = dataInicioUtc, TipoEvento = "Alarme", CodigoEvento = "alarmeON", Valor = estadoAnteriorAlarme.Valor });
            }
            if (estadoAnteriorAviso != null && estadoAnteriorAviso.CodigoEvento == "avisoON")
            {
                todosEventos.Add(new EventoMaquina { Timestamp = dataInicioUtc, TipoEvento = "Aviso", CodigoEvento = "avisoON", Valor = estadoAnteriorAviso.Valor });
            }

            todosEventos.AddRange(eventosDoPeriodo);
            todosEventos = todosEventos.OrderBy(e => e.Timestamp).ToList(); // Re-ordena


            // 4. CALCULAR TEMPO DE CARGA E PERDAS (LÓGICA CORRIGIDA - PASSO 8)
            TimeSpan tempoCargaTotal = TimeSpan.Zero;
            TimeSpan paradasPlanejadasTotal = TimeSpan.Zero;
            TimeSpan perdaPorFalhas = TimeSpan.Zero;
            TimeSpan perdaPorAjustes = TimeSpan.Zero;
            var perdasAgregadas = new Dictionary<string, (TimeSpan TempoTotal, int Frequencia, string Informacao)>();

            // Converte todos os eventos para uma lista simples para processamento
            var eventosProcessados = todosEventos.Select(e => new
            {
                Timestamp = e.Timestamp,
                Tipo = e.TipoEvento,
                Valor = e.Valor,
                Codigo = e.CodigoEvento,
                Estado = (e.CodigoEvento == "alarmeON" || e.CodigoEvento == "avisoON") ? "ON" : "OFF"
            }).ToList();


            // Itera dia a dia no filtro
            for (var dataAtual = dataInicio.Date; dataAtual <= dataFim.Date; dataAtual = dataAtual.AddDays(1))
            {
                // Itera em cada turno do filtro (pode ser "Todos" ou um turno específico)
                foreach (var turno in turnosDoPeriodo)
                {
                    // Define a janela de tempo do turno (ex: 21:30 - 21:40)
                    var inicioTurnoLocal = dataAtual.Date + turno.HoraInicio;
                    var fimTurnoLocal = dataAtual.Date + turno.HoraFim;

                    if (turno.HoraFim <= turno.HoraInicio) // Trata turnos que viram a noite
                    {
                        fimTurnoLocal = fimTurnoLocal.AddDays(1);
                    }

                    // Corta a janela do turno para os limites do filtro de data (ex: 01/01 00:00 e 01/01 23:59)
                    var inicioEfetivoLocal = new[] { inicioTurnoLocal, dataInicio }.Max();
                    var fimEfetivoLocal = new[] { fimTurnoLocal, dataFimAjustada }.Min();

                    // Se a janela do turno é válida (ex: 10 minutos)
                    if (fimEfetivoLocal > inicioEfetivoLocal)
                    {
                        // 4.1. SOMA TEMPO DE CARGA
                        TimeSpan duracaoTurno = fimEfetivoLocal - inicioEfetivoLocal;
                        tempoCargaTotal += duracaoTurno;

                        // 4.2. SOMA PARADAS PLANEJADAS (ex: café, almoço)
                        foreach (var parada in turno.ParadasPlanejadas)
                        {
                            paradasPlanejadasTotal += TimeSpan.FromMinutes(parada.DuracaoMinutos);
                        }

                        // 4.3. CALCULA PERDAS (ALARME/AVISO) DENTRO DESTA JANELA DE TURNO

                        // Converte a janela do turno (Local) para UTC para comparar com os eventos
                        var inicioTurnoUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(inicioEfetivoLocal, DateTimeKind.Unspecified), _tz);
                        var fimTurnoUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fimEfetivoLocal, DateTimeKind.Unspecified), _tz);

                        // Itera sobre todos os eventos (alarmeON, alarmeOFF, avisoON, etc.)
                        for (int i = 0; i < eventosProcessados.Count; i++)
                        {
                            var eventoAtual = eventosProcessados[i];

                            // Apenas calcula se o evento for ON (alarmeON ou avisoON) - (Regras 1 e 2)
                            if (eventoAtual.Estado == "ON")
                            {
                                // 1. Início da Contagem = O timestamp do evento (UTC)
                                DateTime inicioContagemUtc = eventoAtual.Timestamp;

                                // 2. Encontra o próximo evento "OFF" do *mesmo tipo*
                                var proximoEventoOff = eventosProcessados
                                    .Skip(i + 1)
                                    .FirstOrDefault(e => e.Tipo == eventoAtual.Tipo && e.Estado == "OFF");

                                // 3. Fim da Contagem = O evento "OFF" ou, se não houver, o fim do filtro GERAL (dataFimAjustadaUtc)
                                DateTime fimContagemUtc = proximoEventoOff?.Timestamp ?? dataFimAjustadaUtc;

                                // 4. Capping (Corte) da DURAÇÃO para respeitar o TURNO (ex: 21:30 - 21:40 UTC)

                                // O início real é o MAIOR entre (Início do Evento) e (Início do Turno)
                                DateTime inicioCalculo = new[] { inicioContagemUtc, inicioTurnoUtc }.Max();

                                // O fim real é o MENOR entre (Fim da Parada) e (Fim do Turno)
                                DateTime fimCalculo = new[] { fimContagemUtc, fimTurnoUtc }.Min();

                                // 5. Se a janela resultante for válida (fim > início), soma a duração
                                if (fimCalculo > inicioCalculo)
                                {
                                    TimeSpan duracao = fimCalculo - inicioCalculo;

                                    if (eventoAtual.Tipo == "Alarme")
                                    {
                                        perdaPorFalhas += duracao;
                                    }
                                    else // Aviso
                                    {
                                        perdaPorAjustes += duracao;
                                    }

                                    // Agregação para o Top 5
                                    string chaveAgregacao = eventoAtual.Valor ?? eventoAtual.Codigo; // Usa a Descrição (ex: Emergência)
                                    if (perdasAgregadas.TryGetValue(chaveAgregacao, out var agregado))
                                    {
                                        perdasAgregadas[chaveAgregacao] = (agregado.TempoTotal + duracao, agregado.Frequencia + 1, eventoAtual.Valor);
                                    }
                                    else
                                    {
                                        // Apenas adiciona se for a primeira vez que vemos este evento *dentro* da janela do turno
                                        // (Previne contagem de frequência duplicada em múltiplos dias)
                                        if (eventoAtual.Timestamp >= inicioTurnoUtc)
                                        {
                                            perdasAgregadas[chaveAgregacao] = (duracao, 1, eventoAtual.Valor);
                                        }
                                        else
                                        {
                                            perdasAgregadas[chaveAgregacao] = (duracao, 0, eventoAtual.Valor); // Conta duração, mas não frequência (veio de antes)
                                        }
                                    }
                                }
                            }
                        } // Fim do loop de eventos
                    }
                } // Fim do loop de turnos
            } // Fim do loop de dias


            // 5. ATRIBUIÇÃO DE VALORES DE TEMPO
            viewModel.TempoCarga = tempoCargaTotal;
            viewModel.TempoOperacionalPlanejado = tempoCargaTotal - paradasPlanejadasTotal;
            viewModel.TempoParadasNaoPlanejadas = perdaPorFalhas;
            viewModel.TempoParadasAguardando = perdaPorAjustes;

            // 6. CÁLCULO DOS KPIs (Seguindo suas Regras de Negócio)

            // REGRA 3: Tempo de Operação Líquido = TempoCarga - PerdaPorFalhas (Alarmes)
            TimeSpan tempoOperacionalLiquido = viewModel.TempoOperacionalPlanejado - viewModel.TempoParadasNaoPlanejadas;            // Se o tempo líquido for negativo (mais alarme que turno), zera.
            if (tempoOperacionalLiquido < TimeSpan.Zero) tempoOperacionalLiquido = TimeSpan.Zero;

            // REGRA 5: Produção Ideal = TempoCarga * VelocidadeIdeal
            if (viewModel.VelocidadeIdealPorHora > 0)
            {
                viewModel.ContagemIdeal = (long)Math.Round(viewModel.VelocidadeIdealPorHora * viewModel.TempoOperacionalPlanejado.TotalHours);
            }

            // 6.2. Contagem Total (Peças produzidas no período)
            var producaoInicialEvento = await _context.ProducaoInstMaquina
                .Where(p => p.Origem == maquinaId && p.Timestamp <= dataInicioUtc) // <= Início Filtro
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync();

            var producaoFinalEvento = await _context.ProducaoInstMaquina
                .Where(p => p.Origem == maquinaId && p.Timestamp <= dataFimAjustadaUtc) // <= Fim Filtro
                .OrderByDescending(p => p.Timestamp)
                .FirstOrDefaultAsync();

            long valorInicial = 0;
            long valorFinal = 0;
            if (producaoInicialEvento != null && long.TryParse(producaoInicialEvento.Valor, out long inicio))
            {
                valorInicial = inicio;
            }
            if (producaoFinalEvento != null && long.TryParse(producaoFinalEvento.Valor, out long fim))
            {
                valorFinal = fim;
            }
            viewModel.ContagemTotal = Math.Max(0, valorFinal - valorInicial); // REGRA 7 (Produção Bruta)

            // 6.3. Refugo de Qualidade (REGRA 6)
            var refugoEventos = await _context.EventosMaquina
                .Where(e => e.Origem == maquinaId
                    && e.Timestamp >= dataInicioUtc
                    && e.Timestamp <= dataFimAjustadaUtc
                    && e.CodigoEvento == "REFUGO_MANUAL") // Apenas lançamentos manuais
                .ToListAsync();

            viewModel.RefugoQualidade = refugoEventos
                .Sum(e => long.TryParse(e.Valor, out long valor) ? valor : 0L);


            // 6.4. Cálculo do KPI Disponibilidade (A)
            // (Tempo de Operação Líquido / Tempo de Carga)
            if (viewModel.TempoCarga.TotalMinutes > 0)
            {
                viewModel.Disponibilidade = (decimal)(tempoOperacionalLiquido.TotalMinutes / viewModel.TempoCarga.TotalMinutes) * 100;
                viewModel.Disponibilidade = Math.Min(100m, Math.Max(0m, viewModel.Disponibilidade));
            }

            // 6.5. Cálculo do KPI Performance (P)
            // (Produção Total / Produção Ideal) -> REGRA 4 (Perda por Velocidade) está implícita aqui
            if (viewModel.ContagemIdeal > 0)
            {
                viewModel.Performance = (decimal)viewModel.ContagemTotal / viewModel.ContagemIdeal * 100;
                viewModel.Performance = Math.Min(100m, Math.Max(0m, viewModel.Performance));
            }
            else
            {
                viewModel.Performance = (viewModel.ContagemTotal > 0) ? 100m : 0m; // Se não havia ideal, mas produziu, P=100
            }

            // 6.6. Cálculo do KPI Qualidade (Q)
            // (Produção Válida (Líquida) / Produção Total)
            if (viewModel.ContagemTotal > 0)
            {
                long producaoValida = viewModel.ContagemTotal - viewModel.RefugoQualidade; // REGRA 7 (Líquida)
                viewModel.Qualidade = (decimal)producaoValida / viewModel.ContagemTotal * 100;
                viewModel.Qualidade = Math.Min(100m, Math.Max(0m, viewModel.Qualidade));
            }
            else
            {
                viewModel.Qualidade = 100m; // Se não produziu nada, não teve refugo
            }

            // 6.7. CÁLCULO FINAL OEE
            viewModel.OEE = (viewModel.Disponibilidade / 100M) * (viewModel.Performance / 100M) * (viewModel.Qualidade / 100M) * 100M;
            viewModel.OEE = Math.Min(100m, Math.Max(0m, viewModel.OEE));

            // 7. PREPARAR TOP 5 CAUSAS
            viewModel.PerdasNaoPlanejadas = perdasAgregadas
                .Select(kvp => new PerdaDetalheViewModel
                {
                    CodigoEvento = kvp.Key, // Agora é a Descrição (ex: "Emergência Ativa")
                    Descricao = kvp.Key,
                    TempoTotalParado = kvp.Value.TempoTotal,
                    Frequencia = kvp.Value.Frequencia
                })
                .OrderByDescending(p => p.TempoTotalParado)
                .Take(5)
                .ToList();

            return viewModel;
        }
    }
}