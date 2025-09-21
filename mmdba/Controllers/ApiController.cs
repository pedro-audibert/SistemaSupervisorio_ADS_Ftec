/*
=========================================================================================================
ARQUIVO: Controllers/ApiController.cs
FUNÇÃO: Fornece endpoints da API para receber dados em tempo real da Rotuladora.
         Cada endpoint processa a requisição, persiste os dados no banco de dados e envia
         atualizações via SignalR para todos os clientes conectados.

ENDPOINTS:

1. POST /api/mmdba/rotuladora/status
   - Recebe o status atual da máquina.
   - Persiste no banco (EventosMaquina) e envia via StatusHub.

2. POST /api/mmdba/rotuladora/alarmes
   - Recebe alarmes ativos ou desativados.
   - Persiste no banco (EventosMaquina) e envia via AlarmesHub.

3. POST /api/mmdba/rotuladora/avisos
   - Recebe avisos da máquina (informativos).
   - Persiste no banco (EventosMaquina) e envia via AvisosHub.

4. POST /api/mmdba/rotuladora/IOs
   - Recebe eventos de entradas e saídas (IOs) digitais/analógicas.
   - Não persiste no banco (somente envio via IOsHub para atualização em tempo real).

5. POST /api/mmdba/rotuladora/velocidade
   - Recebe velocidade instantânea da máquina.
   - Persiste no banco (VelocidadeInstMaquina) e envia via VelocidadeHub.

6. POST /api/mmdba/rotuladora/contagem
   - Recebe contagem de produção.
   - Persiste no banco (ProducaoInstMaquina) e envia via ContagemHub.

7. POST /api/mmdba/rotuladora/dados
   - Recebe dados gerais ou métricas adicionais da máquina.
   - Persiste no banco (EventosMaquina) e envia via DadosHub.

OBSERVAÇÕES:
- Todos os endpoints utilizam o atributo [ApiKey] para validação de chave de API.
- Todos os timestamps são registrados em UTC.
- O controller registra logs de erro detalhados via ILogger.
- SignalR é utilizado para atualizar todos os clientes conectados em tempo real.
=========================================================================================================
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmdba.Data;
using mmdba.Hubs;
using mmdba.Models;
using mmdba.Models.Entidades;
using System;
using System.Threading.Tasks;

[Route("api/mmdba/")]
[ApiController]
[ApiKey]
public class ApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiController> _logger;
    private readonly IHubContext<StatusHub> _statusHub;
    private readonly IHubContext<AlarmesHub> _alarmesHub;
    private readonly IHubContext<AvisosHub> _avisosHub;
    private readonly IHubContext<IOsHub> _iosHub;
    private readonly IHubContext<VelocidadeHub> _velocidadeHub;
    private readonly IHubContext<DadosHub> _dadosHub;
    private readonly IHubContext<ContagemHub> _contagemHub;

    public ApiController(
        ApplicationDbContext context,
        ILogger<ApiController> logger,
        IHubContext<StatusHub> statusHub,
        IHubContext<AlarmesHub> alarmesHub,
        IHubContext<AvisosHub> avisosHub,
        IHubContext<IOsHub> iosHub,
        IHubContext<VelocidadeHub> velocidadeHub,
        IHubContext<ContagemHub> contagemHub,
        IHubContext<DadosHub> dadosHub)
    {
        _context = context;
        _logger = logger;
        _statusHub = statusHub;
        _alarmesHub = alarmesHub;
        _avisosHub = avisosHub;
        _iosHub = iosHub;
        _velocidadeHub = velocidadeHub;
        _contagemHub = contagemHub;
        _dadosHub = dadosHub;
    }

    [HttpPost("rotuladora/status")]
    public async Task<IActionResult> PostStatusRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoStatus = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };
            _context.EventosMaquina.Add(novoStatus);
            await _context.SaveChangesAsync();
            await _statusHub.Clients.All.SendAsync("postStatus", novoStatus);
            return Ok(new { message = "Status da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar status da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de status da Rotualdora.");
        }
    }

    [HttpPost("rotuladora/alarmes")]
    public async Task<IActionResult> PostAlarmeRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoAlarme = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            _context.EventosMaquina.Add(novoAlarme);
            await _context.SaveChangesAsync(); 
            await _alarmesHub.Clients.All.SendAsync("postAlarmes", novoAlarme);
            return Ok(new { message = "Alarme da máquina recebido e salvo com sucesso." });
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar Alarme da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Alarme da Rotualadora.");
        }
    }

    [HttpPost("rotuladora/avisos")]
    public async Task<IActionResult> PostAvisoRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoAviso = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            _context.EventosMaquina.Add(novoAviso);
            await _context.SaveChangesAsync();
            await _avisosHub.Clients.All.SendAsync("postAvisos", (novoAviso));
            return Ok(new { message = "Aviso da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar aviso da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Aviso da Rotuladora.");
        }
    }

    [HttpPost("rotuladora/IOs")]
    public async Task<IActionResult> PostIOsRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoIOs = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };
            await _iosHub.Clients.All.SendAsync("postIOs", novoIOs);
            return Ok(new { message = "Evento de IO da máquina recebido com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar IOs da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de IOs da Rotuladora.");
        }
    }

    [HttpPost("rotuladora/velocidade")]
    public async Task<IActionResult> PostVelocidadeRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novaVelocidade = new VelocidadeInstMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };
            _context.VelocidadeInstMaquina.Add(novaVelocidade);
            await _context.SaveChangesAsync();
            await _velocidadeHub.Clients.All.SendAsync("postVelocidade", novaVelocidade);
            return Ok(new { message = "Velocidade da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar dados de velocidade da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de dados da Rotuladora.");
        }
    }

    [HttpPost("rotuladora/contagem")]
    public async Task<IActionResult> PostContagemRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novaContagem = new ProducaoInstMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            _context.ProducaoInstMaquina.Add(novaContagem);
            await _context.SaveChangesAsync();
            await _contagemHub.Clients.All.SendAsync("postContagem", novaContagem);
            return Ok(new { message = "Contagem de produção da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar contagem da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Contagem da Rotuladora.");
        }
    }

    [HttpPost("rotuladora/dados")]
    public async Task<IActionResult> PostDadosRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoDado = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            _context.EventosMaquina.Add(novoDado);

            await _context.SaveChangesAsync();
            await _dadosHub.Clients.All.SendAsync("postDados", (novoDado));
            return Ok(new { message = "Dado da maquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar dados da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Dados da Rotuladora.");
        }
    }
}