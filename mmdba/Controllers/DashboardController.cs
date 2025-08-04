using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmdba.Data;
using mmdba.Models; 
using System;
using System.Linq;
using System.Threading.Tasks;

namespace mmdba.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/dashboard")] // Rota base para todos os dados de dashboard
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Busca o histórico de eventos do tipo "Alarme" para a tabela.
        /// </summary>
        [HttpGet("rotuladora/alarmes/historico")]
        public async Task<IActionResult> GetHistoricoAlarmes(int limite = 50)
        {
            var historico = await _context.EventosMaquina
                .Where(e => e.TipoEvento == "Alarme")
                .OrderByDescending(e => e.Timestamp)
                .Take(limite)
                .Select(e => new {
                    id = e.CodigoEvento,
                    valor = e.Valor,
                    informacao = e.Informacao,
                    timestamp = e.Timestamp
                })
                .ToListAsync();

            return Ok(historico);
        }

        /// <summary>
        /// Busca o último estado de alarme da Rotuladora para inicializar o card.
        /// </summary>
        [HttpGet("rotuladora/alarmes/ultimo")]
        public async Task<IActionResult> GetUltimoAlarmeRotuladora()
        {
            var ultimoAlarme = await _context.EventosMaquina
               .Where(e => e.Origem == "Rotuladora" && e.TipoEvento == "Alarme")
               .OrderByDescending(e => e.Timestamp)
               .FirstOrDefaultAsync();

            if (ultimoAlarme != null)
            {
                return Ok(new ApiModel
                {
                    
                    //Id = ultimoAlarme.CodigoEvento,
                    CodigoEvento = ultimoAlarme.CodigoEvento,
                    Valor = ultimoAlarme.Valor,
                    Informacao = ultimoAlarme.Informacao,
                    Timestamp = ultimoAlarme.Timestamp
                    
                });
            }

            // Se não houver nenhum alarme, retorna um estado padrão "Sem Alarmes".
            return Ok(new ApiModel
            {
                //Id = "alarmeOFF",
                CodigoEvento = "alarmeOFF",
                Valor = "Sem Alarmes",
                Informacao = "Nenhum alarme no histórico.",
                Timestamp = DateTime.UtcNow
            });
        }


        /// <summary>
        /// Busca o último estado de status da Rotuladora para inicializar o painel.
        /// </summary>
        [HttpGet("rotuladora/status/ultimo")]
        public async Task<IActionResult> GetUltimoStatusRotuladora()
        {
            var ultimoStatus = await _context.EventosMaquina
               .Where(e => e.Origem == "Rotuladora" && e.TipoEvento == "Status")
               .OrderByDescending(e => e.Timestamp)
               .FirstOrDefaultAsync();

            if (ultimoStatus != null)
            {
                return Ok(new ApiModel
                {
                    //Id = ultimoStatus.CodigoEvento,
                    CodigoEvento = ultimoStatus.CodigoEvento,
                    Valor = ultimoStatus.Valor,
                    Informacao = ultimoStatus.Informacao,
                    Timestamp = ultimoStatus.Timestamp
                });
            }

            return Ok(new ApiModel
            {
                //Id = "statusDesconhecido",
                CodigoEvento = "statusDesconhecido",
                Valor = "Status Desconhecido",
                Informacao = "Nenhum status recebido ainda.",
                Timestamp = DateTime.UtcNow
            });
        }


    }
}