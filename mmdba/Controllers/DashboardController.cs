/* 
=========================================================================================================
ARQUIVO: Controllers/DashboardController.cs
FUNÇÃO: Fornece endpoints de API para alimentar o Painel de Supervisão em tempo real,
         incluindo últimos registros e históricos de Velocidade, Produção, Alarmes, Avisos e Status.
         Suporta filtros por tipo, limite e intervalo de datas.
=========================================================================================================
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmdba.Data;
using mmdba.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace mmdba.Controllers
{
    /// <summary>
    /// Controlador responsável por fornecer endpoints de API
    /// para alimentar o painel de supervisão em tempo real.
    /// Todos os endpoints requerem autenticação.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        /// <summary>
        /// Construtor com injeção de dependência do DbContext e Logger.
        /// </summary>
        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Endpoints de Velocidade

        /// <summary>
        /// Retorna a última velocidade registrada da rotuladora.
        /// </summary>
        [HttpGet("rotuladora/velocidade/ultima")]
        public async Task<IActionResult> GetUltimaVelocidade()
        {
            try
            {
                var ultimaVelocidade = await _context.VelocidadeInstMaquina
                    .AsNoTracking()
                    .OrderByDescending(v => v.Timestamp)
                    .FirstOrDefaultAsync();

                if (ultimaVelocidade == null)
                {
                    // Nenhuma velocidade registrada → retorna 0
                    return Ok(new { Timestamp = DateTime.UtcNow, Valor = 0.0 });
                }

                var resultado = new
                {
                    ultimaVelocidade.Timestamp,
                    Valor = double.Parse(ultimaVelocidade.Valor, CultureInfo.InvariantCulture)
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar a última velocidade.");
                return StatusCode(500, new { Timestamp = DateTime.UtcNow, Valor = 0.0 });
            }
        }

        /// <summary>
        /// Retorna o histórico de velocidade da última hora,
        /// incluindo o ponto imediatamente anterior à janela.
        /// </summary>
        [HttpGet("rotuladora/velocidade/historico")]
        public async Task<IActionResult> GetVelocidadeHistorico()
        {
            try
            {
                var umaHoraAtras = DateTime.UtcNow.AddHours(-1);

                // Pontos dentro da janela de 1 hora
                var pontosNaJanela = await _context.VelocidadeInstMaquina
                    .AsNoTracking()
                    .Where(v => v.Timestamp >= umaHoraAtras)
                    .OrderBy(v => v.Timestamp)
                    .Select(v => new { v.Timestamp, Valor = double.Parse(v.Valor, CultureInfo.InvariantCulture) })
                    .ToListAsync();

                // Último ponto antes da janela (entrada)
                var pontoDeEntrada = await _context.VelocidadeInstMaquina
                    .AsNoTracking()
                    .Where(v => v.Timestamp < umaHoraAtras)
                    .OrderByDescending(v => v.Timestamp)
                    .Select(v => new { v.Timestamp, Valor = double.Parse(v.Valor, CultureInfo.InvariantCulture) })
                    .FirstOrDefaultAsync();

                // Combina ponto de entrada + pontos da janela
                var resultadoFinal = new List<object>();
                if (pontoDeEntrada != null) resultadoFinal.Add(pontoDeEntrada);
                resultadoFinal.AddRange(pontosNaJanela);

                return Ok(resultadoFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o histórico de velocidade.");
                return StatusCode(500, new List<object>());
            }
        }

        #endregion

        #region Endpoint de Produção

        /// <summary>
        /// Retorna o histórico de produção da última hora,
        /// incluindo o ponto imediatamente anterior à janela.
        /// </summary>
        [HttpGet("rotuladora/producao/historico")]
        public async Task<IActionResult> GetProducaoHistorico()
        {
            try
            {
                var umaHoraAtras = DateTime.UtcNow.AddHours(-1);

                // Pontos dentro da janela de 1 hora
                var pontosNaJanela = await _context.ProducaoInstMaquina
                    .AsNoTracking()
                    .Where(p => p.Timestamp >= umaHoraAtras)
                    .OrderBy(p => p.Timestamp)
                    .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                    .ToListAsync();

                // Último ponto antes da janela (entrada)
                var pontoDeEntrada = await _context.ProducaoInstMaquina
                    .AsNoTracking()
                    .Where(p => p.Timestamp < umaHoraAtras)
                    .OrderByDescending(p => p.Timestamp)
                    .Select(p => new { p.Timestamp, Valor = long.Parse(p.Valor) })
                    .FirstOrDefaultAsync();

                // Combina ponto de entrada + pontos da janela
                var resultadoFinal = new List<object>();
                if (pontoDeEntrada != null) resultadoFinal.Add(pontoDeEntrada);
                resultadoFinal.AddRange(pontosNaJanela);

                return Ok(resultadoFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar o histórico de produção.");
                return StatusCode(500, new List<object>());
            }
        }

        #endregion

        #region Endpoints de Eventos

        /// <summary>
        /// Retorna o histórico de eventos da máquina (Alarmes, Avisos, Status),
        /// com suporte a filtros por tipo, limite e intervalo de datas.
        /// </summary>
        [HttpGet("rotuladora/eventos/historico")]
        public async Task<IActionResult> GetHistoricoEventos(
            [FromQuery] string tipos,
            int limite = 100,
            [FromQuery] string dataInicial = null,
            [FromQuery] string dataFinal = null)
        {
            try
            {
                // Define timezone do operador (Brasília)
                TimeZoneInfo tz;
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                }
                catch
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                }

                var query = _context.EventosMaquina.AsNoTracking();

                // Filtro por tipos de eventos
                if (!string.IsNullOrEmpty(tipos))
                {
                    var listaDeTipos = tipos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (listaDeTipos.Any()) query = query.Where(e => listaDeTipos.Contains(e.TipoEvento));
                }

                // Filtro por data inicial (interpreta 00:00 local → UTC)
                if (!string.IsNullOrEmpty(dataInicial))
                {
                    if (DateTime.TryParseExact(dataInicial, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataInicialParsed))
                    {
                        var inicioLocal = dataInicialParsed.Date;
                        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
                        query = query.Where(e => e.Timestamp >= inicioUtc);
                    }
                    else
                    {
                        _logger.LogWarning("Formato de data inicial inválido: {dataInicial}", dataInicial);
                    }
                }

                // Filtro por data final (interpreta 23:59:59.999 local → UTC)
                if (!string.IsNullOrEmpty(dataFinal))
                {
                    if (DateTime.TryParseExact(dataFinal, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataFinalParsed))
                    {
                        var fimLocal = dataFinalParsed.Date.AddDays(1).AddTicks(-1);
                        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(fimLocal, tz);
                        query = query.Where(e => e.Timestamp <= fimUtc);
                    }
                    else
                    {
                        _logger.LogWarning("Formato de data final inválido: {dataFinal}", dataFinal);
                    }
                }

                // Ordena pelos mais recentes
                query = query.OrderByDescending(e => e.Timestamp);

                // Limite aplicado somente se não houver filtro de data
                if (string.IsNullOrEmpty(dataInicial) && string.IsNullOrEmpty(dataFinal))
                {
                    query = query.Take(limite);
                }

                // Retorna os eventos mantendo o mesmo shape esperado pelo front
                var historicoEventos = await query
                    .Select(e => new
                    {
                        e.CodigoEvento,
                        e.Valor,
                        e.Informacao,
                        e.Origem,
                        e.TipoEvento,
                        e.Timestamp
                    })
                    .ToListAsync();

                return Ok(historicoEventos);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao buscar o histórico de eventos. Parâmetros: tipos={tipos}, limite={limite}, dataInicial={dataInicial}, dataFinal={dataFinal}",
                    tipos, limite, dataInicial, dataFinal
                );

                return StatusCode(500, "Erro interno ao buscar o histórico de eventos.");
            }
        }

        /// <summary>
        /// Retorna o último alarme registrado da rotuladora.
        /// </summary>
        [HttpGet("rotuladora/alarmes/ultimo")]
        public async Task<IActionResult> GetUltimoAlarmeRotuladora()
        {
            var ultimoAlarme = await _context.EventosMaquina
                .AsNoTracking()
                .Where(e => e.Origem == "Rotuladora" && e.TipoEvento == "Alarme")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            if (ultimoAlarme != null) return Ok(ultimoAlarme);

            // Retorna objeto default se não houver alarmes
            return Ok(new ApiModel
            {
                CodigoEvento = "alarmeNOT",
                Valor = "Nenhum Alarme Ativo",
                Informacao = "Não há registro do último alarme.",
                Origem = "Rotuladora",
                TipoEvento = "Alarme",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Retorna o último status registrado da rotuladora.
        /// </summary>
        [HttpGet("rotuladora/status/ultimo")]
        public async Task<IActionResult> GetUltimoStatusRotuladora()
        {
            var ultimoStatus = await _context.EventosMaquina
                .AsNoTracking()
                .Where(e => e.Origem == "Rotuladora" && e.TipoEvento == "Status")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefaultAsync();

            if (ultimoStatus != null) return Ok(ultimoStatus);

            // Retorna objeto default se não houver status
            return Ok(new ApiModel
            {
                CodigoEvento = "statusNOT",
                Valor = "Status Desconhecido",
                Informacao = "Não há registro do último status.",
                Origem = "Rotuladora",
                TipoEvento = "Status",
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion
    }
}
