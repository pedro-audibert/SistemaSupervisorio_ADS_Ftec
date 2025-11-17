/*
================================================================================================
ARQUIVO: HomeController.cs
FUNÇÃO:  Controlador principal MVC (Model-View-Controller) responsável por servir as
         páginas (Views) da aplicação e gerar relatórios em PDF/CSV.
================================================================================================
*/

#region NAMESPACES
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mmdba.Models;
using System.Diagnostics;
using mmdba.Data; // Para ApplicationDbContext
using mmdba.Models.Entidades; // Para EventoMaquina
using Microsoft.EntityFrameworkCore; // Para consultas EF Core
using Rotativa.AspNetCore; // Para ViewAsPdf
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http; // Para ter acesso ao HttpContext.Session
using mmdba.Services; // <--- ADICIONE ESTE NAMESPACE (Para IOeeService)#endregion
#endregion

namespace mmdba.Controllers
{
    /// <summary>
    /// Atributo que exige que o usuário esteja autenticado (logado) para 
    /// aceder a qualquer Action deste controlador, exceto as marcadas com [AllowAnonymous].
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        // --- PROPRIEDADES (SERVIÇOS INJETADOS) ---

        /// <summary>
        /// Serviço para registrar logs de informação, aviso ou erro.
        /// </summary>
        private readonly ILogger<HomeController> _logger;

        /// <summary>
        /// Contexto do Banco de Dados (Entity Framework) para consultar tabelas.
        /// </summary>
        private readonly ApplicationDbContext _context;

        private readonly IOeeService _oeeService; // <--- PROPRIEDADE ADICIONADA

        // --- CONSTRUTOR ---

        /// <summary>
        /// Construtor do controlador. A "Injeção de Dependência" (DI) do ASP.NET Core
        /// fornece automaticamente os serviços (logger, dbContext) aqui.
        /// </summary>
        public HomeController(ILogger<HomeController> logger,
                                      ApplicationDbContext context,
                                      IOeeService oeeService) // <--- CORRIGIDO: PARÂMETRO ADICIONADO AQUI
        {
            // Atribui os serviços injetados às propriedades privadas
            _logger = logger;
            _context = context;
            _oeeService = oeeService;
        }

        // --- ACTIONS (PÁGINAS) ---

        /// <summary>
        /// Action (método) que responde à rota /Home/Index.
        /// Exibe a página principal do portal de seleção de máquinas.
        /// </summary>
        /// <returns>O arquivo de View (Views/Home/Index.cshtml)</returns>
        public IActionResult Index()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o portal de seleção de máquinas.", User.Identity?.Name);
            return View(); // Retorna a View 'Index.cshtml'
        }

        /// <summary>
        /// Action que responde à rota /Home/PainelSupervisao.
        /// Exibe o painel de supervisão em tempo real.
        /// </summary>
        /// <returns>O arquivo de View (Views/Home/PainelSupervisao.cshtml)</returns>
        public IActionResult PainelSupervisao(string maquina) // 1. Adicionamos o parâmetro "maquina"
        {
            // 2. Adicionamos a lógica para salvar na Sessão
            if (!string.IsNullOrEmpty(maquina))
            {
                HttpContext.Session.SetString("SelectedMachineId", maquina);
            }
            _logger.LogInformation("Usuário '{UserName}' acessou o Painel de Supervisão da máquina {Maquina}.", User.Identity?.Name, maquina);
            ViewData["Maquina"] = maquina;
            return View();
        }

        /// <summary>
        /// Action que responde à rota /Home/PainelManutencao.
        /// Exibe o painel de manutenção da Rotuladora BOPP.
        /// </summary>
        /// <returns>O arquivo de View (Views/Home/PainelManutencao.cshtml)</returns>
        public IActionResult PainelManutencao()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o Painel de Manutenção da Rotuladora BOPP.", User.Identity?.Name);
            return View();
        }

        /// <summary>
        /// Action que responde à rota /Home/PainelAlarmes.
        /// Exibe o painel de alarmes da Enchedora.
        /// </summary>
        /// <returns>O arquivo de View (Views/Home/PainelAlarmes.cshtml)</returns>
        public IActionResult PainelAlarmes()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o Painel de Alarmes da Enchedora.", User.Identity?.Name);
            return View();
        }


        /// <summary>
        /// Action para exibir o Painel de Análise de OEE (UC6).
        /// </summary>
        public async Task<IActionResult> PainelOEE(
            DateTime? dataInicio,
            DateTime? dataFim,
            int? turnoId) // turnoId da URL (filtro)
        {
            // 1. Obter a máquina selecionada da Sessão
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");

            if (string.IsNullOrEmpty(maquinaId))
            {
                return RedirectToAction("Index", "Home");
            }

            // --- INÍCIO DA LÓGICA DE PERSISTÊNCIA DE TURNO (Passo 5.5 - Robusto) ---

            int idParaUsar;

            if (turnoId.HasValue)
            {
                // 1. Veio um ID da URL (o utilizador clicou em "Recalcular")
                idParaUsar = turnoId.Value;
                // Salva este novo ID na sessão
                HttpContext.Session.SetInt32("SelectedTurnoId", idParaUsar);
            }
            else
            {
                // 2. Não veio ID da URL (F5, reload, ou veio de outro link)
                // Tenta pegar da sessão. Se não existir, GetInt32() retorna null.
                // Usamos '?? 0' para definir "Todos" (ID 0) como padrão.
                idParaUsar = HttpContext.Session.GetInt32("SelectedTurnoId") ?? 0;
            }

            // Passa o ID (da URL ou da Sessão) para a View
            ViewData["SelectedTurnoId"] = idParaUsar;

            // --- FIM DA LÓGICA DE PERSISTÊNCIA ---

            // 2. Definir o período padrão (ex: Hoje)
            var inicio = dataInicio ?? DateTime.Today;
            var fim = dataFim ?? DateTime.Today;

            // 3. Chamar o serviço OEE (usando o idParaUsar)
            var viewModel = await _oeeService.CalcularOEEAsync(
                maquinaId,
                inicio,
                fim,
                idParaUsar
            );

            // 4. Retornar a View com o ViewModel
            return View(viewModel);
        }



        // --- ACTIONS (RELATÓRIOS) ---

        /*
        ================================================================================================
        ACTION: GerarRelatorioEventosPDF
        FUNÇÃO: Gera um relatório de eventos em formato PDF.
                Utiliza a lógica de fuso horário (Time Zone) para corrigir as datas 
                e aplica filtros de data OU limite de eventos (nunca ambos).
        ================================================================================================
        */

        /// <summary>
        /// Action que gera o relatório de eventos em PDF.
        /// Restrita [Authorize] e também requer a Role "Admin".
        /// </summary>
        /// <param name="dataInicio">Filtro de data inicial (opcional).</param>
        /// <param name="dataFim">Filtro de data final (opcional).</param>
        /// <param name="tipoEvento">Filtro por tipo (ex: Alarme, Aviso).</param>
        /// <param name="origem">Filtro por origem (ex: CLP).</param>
        /// <param name="limite">Filtro de "N" últimos eventos (usado se as datas estiverem nulas).</param>
        /// <returns>Um ficheiro PDF gerado pela biblioteca Rotativa.</returns>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarRelatorioEventosPDF(
            DateTime? dataInicio,
            DateTime? dataFim,
            string tipoEvento,
            string origem,
            int? limite)
        {
            // 1. Define o Fuso Horário de Brasília (E. South America) para converter as datas
            //    As datas no banco de dados estão em UTC (Horário Universal).
            TimeZoneInfo tz;
            try
            {
                // Padrão do Windows
                tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            }
            catch
            {
                // Padrão do Linux
                tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            }

            // 2. Inicia a consulta (query) na tabela EventosMaquina.
            //    O 'AsQueryable()' permite que o EF Core monte a consulta SQL.
            var query = _context.EventosMaquina.AsQueryable();

            // 3. Variável de controle para saber se o filtro de data está ativo
            bool isDateFilterActive = dataInicio.HasValue && dataFim.HasValue;

            // 4. Aplicação dos Filtros de Data (se ambos os campos estiverem preenchidos)
            if (isDateFilterActive)
            {
                // Converte a data local (ex: 01/01/2025 00:00:00) para UTC
                var dataInicialLocal = dataInicio.Value.Date;
                var dataInicialUtc = TimeZoneInfo.ConvertTimeToUtc(dataInicialLocal, tz);
                query = query.Where(e => e.Timestamp >= dataInicialUtc);

                // Converte a data local (ex: 01/01/2025 23:59:59) para UTC
                var dataFinalLocal = dataFim.Value.Date.AddDays(1).AddTicks(-1);
                var dataFinalUtc = TimeZoneInfo.ConvertTimeToUtc(dataFinalLocal, tz);
                query = query.Where(e => e.Timestamp <= dataFinalUtc);
            }

            // 5. Aplica os filtros de Tipo (Alarme, Aviso, etc.)
            if (!string.IsNullOrEmpty(tipoEvento) && tipoEvento != "Todos")
            {
                if (!tipoEvento.Equals("Alarme,Aviso,Status", StringComparison.OrdinalIgnoreCase))
                {
                    // Lógica para filtros combinados (ex: "Alarme,Aviso")
                    if (tipoEvento.Contains(','))
                    {
                        var listaDeTipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        query = query.Where(e => listaDeTipos.Contains(e.TipoEvento));
                    }
                    else // Lógica para filtro único
                    {
                        query = query.Where(e => e.TipoEvento == tipoEvento);
                    }
                }
            }

            // 6. Aplica o filtro de Origem (ex: "Rotuladora")
            if (!string.IsNullOrEmpty(origem))
            {
                query = query.Where(e => e.Origem.Contains(origem));
            }

            // 7. Ordena sempre pelos eventos mais recentes
            query = query.OrderByDescending(e => e.Timestamp);

            // 8. Aplica o Limite (N últimos) APENAS se o filtro de data NÃO estiver ativo
            if (!isDateFilterActive && limite.HasValue && limite.Value > 0)
            {
                // Usa o 'Take()' do SQL para pegar apenas os 'limite' primeiros
                query = query.Take(limite.Value);
            }

            // 9. Executa a consulta no banco de dados (o 'await' envia o SQL)
            var eventos = await query.ToListAsync();

            // 10. Configura o nome e as opções do PDF
            string fileName = $"Relatorio_Eventos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // 11. Retorna o PDF usando a biblioteca Rotativa
            return new ViewAsPdf("RelatorioEventosView", eventos) // Usa a View 'RelatorioEventosView.cshtml'
            {
                FileName = fileName,
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape,
                CustomSwitches = "--footer-right \"Página [page] de [topage]\" --footer-font-size \"10\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Action que retorna a lista de turnos cadastrados para a máquina na sessão (para filtros AJAX).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObterTurnosMaquina()
        {
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");

            if (string.IsNullOrEmpty(maquinaId))
            {
                // Retorna 404 (Not Found) se não houver máquina selecionada
                return NotFound();
            }

            try
            {
                // Busca os turnos no banco de dados
                var turnos = await _context.Turnos
                    .Where(t => t.MaquinaId == maquinaId) //
                    .OrderBy(t => t.HoraInicio)
                    .Select(t => new TurnoViewModel // Projeta para o modelo simplificado
                    {
                        Id = t.Id,
                        Nome = t.Nome
                    })
                    .ToListAsync();

                // Retorna a lista em JSON. O ASP.NET Core cuidará da serialização.
                return Json(turnos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao obter lista de turnos para a máquina {Maquina}.", maquinaId);
                return StatusCode(500, "Erro interno ao processar a requisição de turnos.");
            }
        }

        /*
        ================================================================================================
        ACTION: GerarRelatorioEventosCSV
        FUNÇÃO: Gera o relatório em formato CSV (Texto, compatível com Excel)
                usando os mesmos filtros de data/limite da Action do PDF.
        ================================================================================================
        */

        /// <summary>
        /// Action que gera o relatório de eventos em CSV.
        /// Restrita [Authorize] e também requer a Role "Admin".
        /// </summary>
        /// <returns>Um ficheiro CSV (text/csv) para download.</returns>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarRelatorioEventosCSV(
            DateTime? dataInicio,
            DateTime? dataFim,
            string tipoEvento,
            string origem,
            int? limite)
        {
            // --- 1. Lógica de Filtragem (Exatamente igual ao GerarRelatorioEventosPDF) ---

            TimeZoneInfo tz;
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
            catch { tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }

            var query = _context.EventosMaquina.AsQueryable();
            bool isDateFilterActive = dataInicio.HasValue && dataFim.HasValue;

            // Filtros de Data
            if (isDateFilterActive)
            {
                var dataInicialLocal = dataInicio.Value.Date;
                var dataInicialUtc = TimeZoneInfo.ConvertTimeToUtc(dataInicialLocal, tz);
                query = query.Where(e => e.Timestamp >= dataInicialUtc);

                var dataFinalLocal = dataFim.Value.Date.AddDays(1).AddTicks(-1);
                var dataFinalUtc = TimeZoneInfo.ConvertTimeToUtc(dataFinalLocal, tz);
                query = query.Where(e => e.Timestamp <= dataFinalUtc);
            }

            // Filtros de Tipo e Origem
            if (!string.IsNullOrEmpty(tipoEvento) && tipoEvento != "Todos")
            {
                if (!tipoEvento.Equals("Alarme,Aviso,Status", StringComparison.OrdinalIgnoreCase))
                {
                    if (tipoEvento.Contains(','))
                    {
                        var listaDeTipos = tipoEvento.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        query = query.Where(e => listaDeTipos.Contains(e.TipoEvento));
                    }
                    else
                    {
                        query = query.Where(e => e.TipoEvento == tipoEvento);
                    }
                }
            }
            if (!string.IsNullOrEmpty(origem))
            {
                query = query.Where(e => e.Origem.Contains(origem));
            }

            // Ordena e aplica o limite (se não houver filtro de data)
            query = query.OrderByDescending(e => e.Timestamp);
            if (!isDateFilterActive && limite.HasValue && limite.Value > 0)
            {
                query = query.Take(limite.Value);
            }

            // Executa a consulta
            var eventos = await query.ToListAsync();

            // --- 2. Formatação CSV ---

            // Cria um "escritor de string" (StringWriter) para construir o arquivo em memória
            using (var writer = new StringWriter())
            {
                // Escreve o Cabeçalho CSV. 
                // Usamos ponto-e-vírgula (;) como separador para compatibilidade com Excel em PT-BR.
                writer.WriteLine("Data/Hora;Origem;Tipo;Codigo;Informacao;Valor");

                // Itera sobre cada evento retornado do banco
                foreach (var evento in eventos)
                {
                    // Converte o Timestamp (UTC) para a hora local (Brasília) para o relatório
                    string localTimestamp = TimeZoneInfo.ConvertTimeFromUtc(evento.Timestamp, tz).ToString("dd/MM/yyyy HH:mm:ss");

                    // Limpa os campos de texto para evitar quebras de linha ou (;) que corrompam o CSV
                    string informacao = evento.Informacao?.Replace(";", " ").Replace("\r\n", " ").Replace('\n', ' ') ?? "";
                    string valor = evento.Valor?.Replace(";", " ").Replace("\r\n", " ").Replace('\n', ' ') ?? "";

                    // Escreve a linha de dados formatada
                    writer.WriteLine($"{localTimestamp};{evento.Origem};{evento.TipoEvento};{evento.CodigoEvento};{informacao};{valor}");
                }

                // Define o nome do arquivo
                var fileName = $"Relatorio_Eventos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                // Converte a string (UTF-8) em um array de bytes
                var csvData = Encoding.UTF8.GetBytes(writer.ToString());

                // Retorna o ficheiro CSV para o navegador
                return File(csvData, "text/csv", fileName);
            }
        }


        // --- ACTIONS PADRÃO (PRIVACY, ERROR) ---

        /// <summary>
        /// Action que exibe a página de política de privacidade.
        /// [AllowAnonymous] permite que usuários NÃO logados acedam a esta página.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            _logger.LogInformation("Visitante acessou a página de Privacidade.");
            return View();
        }

        /// <summary>
        /// Action que exibe a página de erro genérica.
        /// [AllowAnonymous] permite o acesso público.
        /// [ResponseCache] garante que esta página nunca seja salva em cache,
        /// para que sempre mostre o erro mais recente.
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Pega o ID da requisição (Request ID) para rastreio do erro
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Página de erro exibida. RequestId: {RequestId}", requestId);

            // Retorna a View de Erro, passando o RequestId para ser exibido
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}