using Microsoft.EntityFrameworkCore;
using mmdba.Data;
using mmdba.Models;
using mmdba.Models.Entidades;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mmdba.Services
{
    public class OeeService : IOeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OeeService> _logger;
        private readonly TimeZoneInfo _tz;

        public OeeService(ApplicationDbContext context, ILogger<OeeService> logger)
        {
            _context = context;
            _logger = logger;
            try { _tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
            catch { _tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        }

        public async Task<AnaliseOEEViewModel> CalcularOEEAsync(string maquinaId, DateTime dataInicio, DateTime dataFim, int? turnoId)
        {
            // 1. Definições de Tempo
            var inicioSemFuso = DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Unspecified);
            var fimSemFuso = DateTime.SpecifyKind(dataFim.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);

            var dataInicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioSemFuso, _tz);
            var dataFimUtc = TimeZoneInfo.ConvertTimeToUtc(fimSemFuso, _tz);

            var agoraUtc = DateTime.UtcNow;
            var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, _tz);

            var viewModel = new AnaliseOEEViewModel
            {
                MaquinaId = maquinaId,
                DataInicio = inicioSemFuso,
                DataFim = fimSemFuso,
                PerdasNaoPlanejadas = new List<PerdaDetalheViewModel>()
            };

            if (dataInicioUtc > agoraUtc) return viewModel;

            // 2. Carregar Configurações e Turnos
            var parametros = await _context.OeeParametrosMaquina.AsNoTracking().FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);
            viewModel.VelocidadeIdealPorHora = parametros?.VelocidadeIdealPorHora ?? 0;
            viewModel.HabilitarRefugoManual = parametros?.HabilitarRefugoManual ?? false;
            viewModel.TaxaAtualizacaoMinutos = parametros?.TaxaAtualizacaoMinutos ?? 10;

            IQueryable<Turno> queryTurnos = _context.Turnos.AsNoTracking().Where(t => t.MaquinaId == maquinaId);
            if (turnoId.HasValue && turnoId.Value > 0) queryTurnos = queryTurnos.Where(t => t.Id == turnoId.Value);
            var turnosDefinidos = await queryTurnos.Include(t => t.ParadasPlanejadas).ToListAsync();

            if (!turnosDefinidos.Any() && (!turnoId.HasValue || turnoId.Value == 0))
            {
                turnosDefinidos.Add(new Turno { Id = 0, Nome = "Turno Padrão (24h)", HoraInicio = TimeSpan.Zero, HoraFim = TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1), ParadasPlanejadas = new List<ParadaPlanejada>() });
            }

            // 3. Buscar Dados
            var dbStartUtc = dataInicioUtc.AddHours(-24);
            var dbEndUtc = dataFimUtc;

            var eventosDb = await _context.EventosMaquina.AsNoTracking()
                .Where(e => e.Origem == maquinaId && e.Timestamp >= dbStartUtc && e.Timestamp <= dbEndUtc)
                .OrderBy(e => e.Timestamp)
                .Select(e => new { e.Timestamp, e.TipoEvento, e.CodigoEvento, e.Valor })
                .ToListAsync();

            var producaoDb = await _context.ProducaoInstMaquina.AsNoTracking()
                .Where(p => p.Origem == maquinaId && p.Timestamp >= dbStartUtc && p.Timestamp <= dbEndUtc)
                .OrderBy(p => p.Timestamp)
                .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                .ToListAsync();

            // 4. Algoritmo

            // Variáveis de Display (TOTAL AGENDADO)
            TimeSpan tempoBrutoTotalDisplay = TimeSpan.Zero;
            TimeSpan tempoParadasPlanejadasTotalDisplay = TimeSpan.Zero;

            // Variáveis de OEE (REAL DECORRIDO)
            TimeSpan tempoPlanejadoDecorrido = TimeSpan.Zero;
            TimeSpan tempoFalhaTotal = TimeSpan.Zero;
            TimeSpan tempoAvisoTotal = TimeSpan.Zero;
            long producaoTotal = 0;

            var perdasDict = new Dictionary<string, (TimeSpan Tempo, int Freq)>();

            for (var dia = inicioSemFuso; dia <= fimSemFuso.Date; dia = dia.AddDays(1))
            {
                foreach (var turno in turnosDefinidos)
                {
                    // A. Janela do Turno
                    DateTime inicioTurnoDt = dia.Add(turno.HoraInicio);
                    DateTime fimTurnoDt = dia.Add(turno.HoraFim);
                    if (fimTurnoDt <= inicioTurnoDt) fimTurnoDt = fimTurnoDt.AddDays(1);

                    // B. Interseção com Filtro (Janela Total)
                    DateTime inicioJanela = inicioTurnoDt > inicioSemFuso ? inicioTurnoDt : inicioSemFuso;
                    DateTime fimJanela = fimTurnoDt < fimSemFuso ? fimTurnoDt : fimSemFuso;

                    if (inicioJanela >= fimJanela) continue;

                    // --- CÁLCULO DE CARGA E PARADAS (TOTAL) ---
                    // Calcula a duração bruta do turno neste dia
                    TimeSpan duracaoBruta = fimJanela - inicioJanela;

                    // Soma as paradas planejadas cadastradas (ex: 10 min)
                    TimeSpan paradasDoTurno = TimeSpan.Zero;
                    foreach (var pp in turno.ParadasPlanejadas) paradasDoTurno += TimeSpan.FromMinutes(pp.DuracaoMinutos);

                    // Trava de segurança: paradas não podem ser maiores que o turno
                    if (paradasDoTurno > duracaoBruta) paradasDoTurno = duracaoBruta;

                    tempoBrutoTotalDisplay += duracaoBruta;
                    tempoParadasPlanejadasTotalDisplay += paradasDoTurno;
                    // -------------------------------------------

                    // C. Janela Decorrida (OEE)
                    DateTime fimDecorrido = fimJanela > agoraLocal ? agoraLocal : fimJanela;
                    if (inicioJanela >= fimDecorrido) continue;

                    TimeSpan duracaoDecorrida = fimDecorrido - inicioJanela;

                    // Desconta paradas proporcionalmente (ou total se já passou) para o OEE
                    // OBS: Para OEE mantemos a lógica de descontar do tempo disponível
                    TimeSpan paradasDecorrido = paradasDoTurno;
                    if (paradasDecorrido > duracaoDecorrida) paradasDecorrido = duracaoDecorrida;

                    tempoPlanejadoDecorrido += (duracaoDecorrida - paradasDecorrido);

                    // D. Eventos (Alarmes/Avisos)
                    var janelaStartUtc = TimeZoneInfo.ConvertTimeToUtc(inicioJanela, _tz);
                    var janelaEndUtc = TimeZoneInfo.ConvertTimeToUtc(fimDecorrido, _tz);

                    var estadoAlarme = "OFF";
                    var ultimoAlarmeCodigo = "";
                    var evAntAlarme = eventosDb.Where(e => e.Timestamp < janelaStartUtc && e.TipoEvento == "Alarme").LastOrDefault();
                    if (evAntAlarme != null && evAntAlarme.CodigoEvento == "alarmeON") { estadoAlarme = "ON"; ultimoAlarmeCodigo = evAntAlarme.Valor; }

                    var estadoAviso = "OFF";
                    var evAntAviso = eventosDb.Where(e => e.Timestamp < janelaStartUtc && e.TipoEvento == "Aviso").LastOrDefault();
                    if (evAntAviso != null && evAntAviso.CodigoEvento == "avisoON") { estadoAviso = "ON"; }

                    var eventosNaJanela = eventosDb
                        .Where(e => e.Timestamp >= janelaStartUtc && e.Timestamp <= janelaEndUtc &&
                                   (e.TipoEvento == "Alarme" || e.TipoEvento == "Aviso"))
                        .ToList();

                    var pontosTempo = new List<DateTime> { janelaStartUtc, janelaEndUtc };
                    pontosTempo.AddRange(eventosNaJanela.Select(e => e.Timestamp));
                    pontosTempo = pontosTempo.Distinct().OrderBy(t => t).ToList();

                    for (int i = 0; i < pontosTempo.Count - 1; i++)
                    {
                        var t1 = pontosTempo[i];
                        var duration = pontosTempo[i + 1] - t1;
                        var eventosT1 = eventosNaJanela.Where(e => e.Timestamp == t1).ToList();

                        foreach (var ev in eventosT1)
                        {
                            if (ev.TipoEvento == "Alarme") { estadoAlarme = (ev.CodigoEvento == "alarmeON") ? "ON" : "OFF"; if (estadoAlarme == "ON") ultimoAlarmeCodigo = ev.Valor; }
                            else if (ev.TipoEvento == "Aviso") { if (ev.CodigoEvento == "avisoON") estadoAviso = "ON"; else if (ev.CodigoEvento == "avisoOFF") estadoAviso = "OFF"; }
                        }

                        if (estadoAlarme == "ON")
                        {
                            tempoFalhaTotal += duration;
                            var key = ultimoAlarmeCodigo ?? "Desconhecido";
                            if (!perdasDict.ContainsKey(key)) perdasDict[key] = (TimeSpan.Zero, 0);
                            var (t, f) = perdasDict[key];
                            perdasDict[key] = (t + duration, f);
                        }
                        if (estadoAviso == "ON") tempoAvisoTotal += duration;
                    }

                    foreach (var ev in eventosNaJanela.Where(e => e.CodigoEvento == "alarmeON"))
                    {
                        var key = ev.Valor ?? "Desconhecido";
                        if (!perdasDict.ContainsKey(key)) perdasDict[key] = (TimeSpan.Zero, 0);
                        var (t, f) = perdasDict[key];
                        perdasDict[key] = (t, f + 1);
                    }

                    // E. Produção
                    var contagemInicio = producaoDb.Where(p => p.Timestamp <= janelaStartUtc).OrderByDescending(p => p.Timestamp).FirstOrDefault()?.Valor ?? 0;
                    var contagemFim = producaoDb.Where(p => p.Timestamp <= janelaEndUtc).OrderByDescending(p => p.Timestamp).FirstOrDefault()?.Valor ?? 0;
                    if (contagemFim >= contagemInicio) producaoTotal += (contagemFim - contagemInicio); else producaoTotal += contagemFim;
                }
            }

            var refugoTotal = eventosDb.Where(e => e.CodigoEvento == "REFUGO_MANUAL" && e.Timestamp >= TimeZoneInfo.ConvertTimeToUtc(inicioSemFuso, _tz)).Sum(e => long.TryParse(e.Valor, out long v) ? v : 0);

            // --- RESULTADOS DE EXIBIÇÃO ---
            // Tempo Carga (Exibição) = Tempo Bruto Total - Paradas Planejadas Totais
            viewModel.TempoCarga = tempoBrutoTotalDisplay - tempoParadasPlanejadasTotalDisplay;

            // Nova propriedade para exibição fixa
            viewModel.TempoParadasPlanejadas = tempoParadasPlanejadasTotalDisplay;

            // --- RESULTADOS DE OEE (Decorrido) ---
            viewModel.TempoOperacionalPlanejado = tempoPlanejadoDecorrido;
            viewModel.TempoOperacionalLiquido = (tempoPlanejadoDecorrido - tempoFalhaTotal) < TimeSpan.Zero ? TimeSpan.Zero : (tempoPlanejadoDecorrido - tempoFalhaTotal);
            viewModel.TempoParadasNaoPlanejadas = tempoFalhaTotal;
            viewModel.TempoParadasAguardando = tempoAvisoTotal;
            viewModel.ContagemTotal = producaoTotal;
            viewModel.RefugoQualidade = refugoTotal;

            if (viewModel.TempoOperacionalPlanejado.TotalSeconds > 0)
                viewModel.Disponibilidade = (decimal)(viewModel.TempoOperacionalLiquido.TotalSeconds / viewModel.TempoOperacionalPlanejado.TotalSeconds) * 100m;

            double horasOperando = viewModel.TempoOperacionalLiquido.TotalHours;
            if (viewModel.VelocidadeIdealPorHora > 0 && horasOperando > 0)
            {
                double prodTeorica = horasOperando * viewModel.VelocidadeIdealPorHora;
                viewModel.ContagemIdeal = (long)prodTeorica;
                if (prodTeorica > 0) viewModel.Performance = (decimal)(viewModel.ContagemTotal / prodTeorica) * 100m;
            }
            else if (horasOperando > 0 && viewModel.ContagemTotal > 0) viewModel.Performance = 100m;

            if (viewModel.ContagemTotal > 0)
            {
                long boas = viewModel.ContagemTotal - viewModel.RefugoQualidade;
                viewModel.Qualidade = (decimal)((double)boas / viewModel.ContagemTotal) * 100m;
            }
            else viewModel.Qualidade = 100m;

            viewModel.OEE = (viewModel.Disponibilidade / 100m) * (viewModel.Performance / 100m) * (viewModel.Qualidade / 100m) * 100m;

            viewModel.OEE = Math.Clamp(viewModel.OEE, 0, 100);
            viewModel.Disponibilidade = Math.Clamp(viewModel.Disponibilidade, 0, 100);
            viewModel.Performance = Math.Clamp(viewModel.Performance, 0, 100);
            viewModel.Qualidade = Math.Clamp(viewModel.Qualidade, 0, 100);

            viewModel.PerdasNaoPlanejadas = perdasDict.Select(x => new PerdaDetalheViewModel { CodigoEvento = "ALM", Descricao = x.Key, TempoTotalParado = x.Value.Tempo, Frequencia = x.Value.Freq }).OrderByDescending(x => x.TempoTotalParado).Take(5).ToList();

            return viewModel;
        }
    }
}