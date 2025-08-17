using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmdba.Data;
using mmdba.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace mmdba.Controllers
{
    /// <summary>
    /// Fornece endpoints de API para alimentar os dados do painel de supervisão em tempo real.
    /// Requer autorização para todos os endpoints.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        /// <summary>
        /// Inicializa uma nova instância do controlador com injeção de dependência.
        /// </summary>
        /// <param name="context">O contexto do banco de dados (Entity Framework).</param>
        /// <param name="logger">O serviço de logging.</param>
        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Endpoints de Velocidade

        /// <summary>
        /// ROTA: GET /api/dashboard/rotuladora/velocidade/ultima
        /// Busca o último registro de velocidade instantânea da máquina.
        /// </summary>
        /// <returns>Um objeto com Timestamp e Valor, ou um valor padrão de 0 se não houver dados ou em caso de erro.</returns>
        [HttpGet("rotuladora/velocidade/ultima")]
        public async Task<IActionResult> GetUltimaVelocidade()
        {
            try
            {
                var ultimaVelocidade = await _context.VelocidadeInstMaquina
                    .AsNoTracking() // Otimização de performance para consultas de apenas leitura.
                    .OrderByDescending(v => v.Timestamp)
                    .FirstOrDefaultAsync();

                if (ultimaVelocidade == null)
                {
                    // Se não houver nenhum registro no banco, retorna um estado padrão para não quebrar o frontend.
                    return Ok(new { Timestamp = DateTime.UtcNow, Valor = 0 });
                }

                // Cria um objeto anônimo para retornar apenas os dados necessários.
                var resultado = new
                {
                    ultimaVelocidade.Timestamp,
                    // Converte o valor (string) para double, usando CultureInfo.InvariantCulture para garantir o uso do ponto '.' como separador decimal.
                    Valor = double.Parse(ultimaVelocidade.Valor, CultureInfo.InvariantCulture)
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar a última velocidade.");
                // Em caso de falha (ex: erro de parsing), retorna um status 500 com um valor padrão para manter a estabilidade do frontend.
                return StatusCode(500, new { Timestamp = DateTime.UtcNow, Valor = 0 });
            }
        }

        /// <summary>
        /// ROTA: GET /api/dashboard/rotuladora/velocidade/historico
        /// Busca o histórico de velocidade da última hora. Se não houver, retorna o último ponto registrado.
        /// </summary>

        /*
        [HttpGet("rotuladora/velocidade/historico")]
        public async Task<IActionResult> GetVelocidadeHistorico()
        {
            try
            {
                // var trintaMinutosAtras = DateTime.UtcNow.AddMinutes(-30); <- Caso queira mudar a base de tepo da consulta
                var umaHoraAtras = DateTime.UtcNow.AddHours(-1);

                var dadosHistoricos = await _context.VelocidadeInstMaquina
                    .AsNoTracking()
                    .Where(v => v.Timestamp >= umaHoraAtras)
                    .OrderBy(v => v.Timestamp)
                    .Select(v => new { v.Timestamp, Valor = double.Parse(v.Valor, CultureInfo.InvariantCulture) })
                    .ToListAsync();

                if (!dadosHistoricos.Any())
                {
                    var ultimoPonto = await _context.VelocidadeInstMaquina
                        .AsNoTracking()
                        .OrderByDescending(v => v.Timestamp)
                        .Select(v => new { v.Timestamp, Valor = double.Parse(v.Valor, CultureInfo.InvariantCulture) })
                        .FirstOrDefaultAsync();

                    if (ultimoPonto != null)
                    {
                        dadosHistoricos.Add(ultimoPonto);
                    }
                }

                return Ok(dadosHistoricos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o histórico de velocidade.");
                return StatusCode(500, "Erro interno ao buscar histórico de velocidade.");
            }
        }
        */

        [HttpGet("rotuladora/velocidade/historico")]
        public async Task<IActionResult> GetVelocidadeHistorico()
        {
            try
            {
                var umaHoraAtras = DateTime.UtcNow.AddHours(-1);

                // 1. Busca os pontos DENTRO da janela de tempo.
                var pontosNaJanela = await _context.VelocidadeInstMaquina
                    .AsNoTracking()
                    .Where(v => v.Timestamp >= umaHoraAtras)
                    .OrderBy(v => v.Timestamp)
                    .Select(v => new { v.Timestamp, Valor = double.Parse(v.Valor, CultureInfo.InvariantCulture) })
                    .ToListAsync();

                // 2. Busca o PRIMEIRO ponto ANTES da janela de tempo, para servir de âncora.
                var pontoDeEntrada = await _context.VelocidadeInstMaquina
                    .AsNoTracking()
                    .Where(v => v.Timestamp < umaHoraAtras)
                    .OrderByDescending(v => v.Timestamp)
                    .Select(v => new { v.Timestamp, Valor = double.Parse(v.Valor, CultureInfo.InvariantCulture) })
                    .FirstOrDefaultAsync();

                // 3. Combina os resultados para enviar a lista perfeita para o frontend.
                var resultadoFinal = new List<object>();
                if (pontoDeEntrada != null)
                {
                    resultadoFinal.Add(pontoDeEntrada);
                }
                resultadoFinal.AddRange(pontosNaJanela);

                return Ok(resultadoFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o histórico de velocidade.");
                return StatusCode(500, "Erro interno ao buscar histórico de velocidade.");
            }
        }

        #endregion

        #region Endpoint de Produção

        /// <summary>
        /// ROTA: GET /api/dashboard/rotuladora/producao/historico
        /// Busca o histórico de produção da última hora. Se não houver, retorna o último ponto registrado.
        /// </summary>
        /*
        [HttpGet("rotuladora/producao/historico")]
        public async Task<IActionResult> GetProducaoHistorico()
        {
            try
            {
                // CORREÇÃO: Alterado de DateTime.Now para DateTime.UtcNow para seguir o padrão do projeto.
                var umaHoraAtras = DateTime.UtcNow.AddHours(-1);

                var dadosHistoricos = await _context.ProducaoInstMaquina
                    .AsNoTracking()
                    .Where(p => p.Timestamp >= umaHoraAtras)
                    .OrderBy(p => p.Timestamp)
                    .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                    .ToListAsync();

                // MELHORIA: Se não houver dados na última hora, busca o último ponto válido.
                if (!dadosHistoricos.Any())
                {
                    var ultimoPonto = await _context.ProducaoInstMaquina
                        .AsNoTracking()
                        .OrderByDescending(p => p.Timestamp)
                        .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                        .FirstOrDefaultAsync();

                    if (ultimoPonto != null)
                    {
                        dadosHistoricos.Add(ultimoPonto);
                    }
                }

                return Ok(dadosHistoricos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o histórico de produção.");
                return StatusCode(500, "Erro interno ao buscar histórico de produção.");
            }
        }
        */
        [HttpGet("rotuladora/producao/historico")]
        public async Task<IActionResult> GetProducaoHistorico()
        {
            try
            {
                var umaHoraAtras = DateTime.UtcNow.AddHours(-1);

                var pontosNaJanela = await _context.ProducaoInstMaquina
                    .AsNoTracking()
                    .Where(p => p.Timestamp >= umaHoraAtras)
                    .OrderBy(p => p.Timestamp)
                    .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                    .ToListAsync();

                var pontoDeEntrada = await _context.ProducaoInstMaquina
                    .AsNoTracking()
                    .Where(p => p.Timestamp < umaHoraAtras)
                    .OrderByDescending(p => p.Timestamp)
                    .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                    .FirstOrDefaultAsync();

                var resultadoFinal = new List<object>();
                if (pontoDeEntrada != null)
                {
                    resultadoFinal.Add(pontoDeEntrada);
                }
                resultadoFinal.AddRange(pontosNaJanela);

                return Ok(resultadoFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o histórico de produção.");
                return StatusCode(500, "Erro interno ao buscar histórico de produção.");
            }
        }


        #endregion

        #region Endpoints de Eventos (Status e Alarmes)

        /// <summary>
        /// ROTA: GET /api/dashboard/rotuladora/alarmes/historico
        /// Busca os últimos eventos de alarme registrados.
        /// </summary>
        /// <param name="limite">A quantidade máxima de alarmes a serem retornados (padrão: 100).</param>
        /// <returns>Uma lista dos últimos eventos de alarme.</returns>
        [HttpGet("rotuladora/alarmes/historico")]
        public async Task<IActionResult> GetHistoricoAlarmes(int limite = 100)
        {
            var historicoAlarme = await _context.EventosMaquina
                .AsNoTracking()
                .Where(e => e.TipoEvento == "Alarme")
                .OrderByDescending(e => e.Timestamp)
                .Take(limite)
                .Select(e => new {
                    e.CodigoEvento,
                    e.Valor,
                    e.Informacao,
                    e.Origem,
                    e.TipoEvento,
                    e.Timestamp
                })
                .ToListAsync();

            return Ok(historicoAlarme);
        }

        /// <summary>
        /// ROTA: GET /api/dashboard/rotuladora/alarmes/ultimo
        /// Busca o último evento de alarme da rotuladora.
        /// </summary>
        /// <returns>O último alarme ou um objeto padrão indicando que nenhum alarme foi encontrado.</returns>
        [HttpGet("rotuladora/alarmes/ultimo")]
        public async Task<IActionResult> GetUltimoAlarmeRotuladora()
        {
            var ultimoAlarme = await _context.EventosMaquina
                .AsNoTracking()
                .Where(e => e.Origem == "Rotuladora" && e.TipoEvento == "Alarme")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            if (ultimoAlarme != null)
            {
                return Ok(ultimoAlarme); // Retorna o objeto completo do modelo
            }

            // Retorna um objeto padrão se nenhum alarme for encontrado, para manter a consistência no frontend.
            return Ok(new ApiModel
            {
                CodigoEvento = "alarmeNOT",
                Valor = "Alarme Não Encontrado",
                Informacao = "Não foi encontrado o último alarme no DB",
                Origem = "Rotuladora",
                TipoEvento = "Alarme",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// ROTA: GET /api/dashboard/rotuladora/status/ultimo
        /// Busca o último evento de status da rotuladora.
        /// </summary>
        /// <returns>O último status ou um objeto padrão indicando que nenhum status foi encontrado.</returns>
        [HttpGet("rotuladora/status/ultimo")]
        public async Task<IActionResult> GetUltimoStatusRotuladora()
        {
            var ultimoStatus = await _context.EventosMaquina
                .AsNoTracking()
                .Where(e => e.Origem == "Rotuladora" && e.TipoEvento == "Status")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            if (ultimoStatus != null)
            {
                return Ok(ultimoStatus);
            }

            // Retorna um objeto padrão se nenhum status for encontrado.
            return Ok(new ApiModel
            {
                CodigoEvento = "statusNOT",
                Valor = "Status Não Encontrado",
                Informacao = "Não foi encontrado o último status no DB",
                Origem = "Rotuladora",
                TipoEvento = "Status",
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion
    }
}