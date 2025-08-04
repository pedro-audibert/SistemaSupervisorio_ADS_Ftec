/*
=========================================================================================================
ARQUIVO: ApiController.cs (VERSÃO FINAL COM PERSISTÊNCIA)
FUNÇÃO:  Este controller é o ponto de entrada da API. Sua responsabilidade agora é dupla:
         1. RECEBER dados de sistemas externos (como o Node-RED).
         2. PERSISTIR esses dados no banco de dados, na tabela 'EventosMaquina'.
         3. DISTRIBUIR os dados em tempo real para os clientes conectados via SignalR.
=========================================================================================================
*/

// Namespaces necessários para o funcionamento do controller, do SignalR, Logging e acesso a dados.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mmdba.Data; // Adicionado para permitir o acesso ao ApplicationDbContext e ao banco de dados.
using mmdba.Hubs;
using mmdba.Models;
using mmdba.Models.Entidades;
using System;
using System.Threading.Tasks;

// Define a rota base para todos os endpoints deste controller. Ex: /api/mmdba/...
[Route("api/mmdba/")]
// Atributo que habilita funcionalidades específicas de API, como a inferência de fonte de parâmetros.
[ApiController]
// Atributo que aplica uma chave de API para autenticação. Apenas requisições com a chave correta são permitidas.
[ApiKey]
public class ApiController : ControllerBase
{
    // ===================================================================================
    // 1. CAMPOS E INJEÇÃO DE DEPENDÊNCIA (VERSÃO COMPLETA)
    // ===================================================================================

    // Campos privados para armazenar as instâncias dos serviços que serão injetados.
    // O 'readonly' garante que eles só podem ser atribuídos no construtor.
    private readonly ApplicationDbContext _context; // Para interagir com o banco de dados.
    private readonly ILogger<ApiController> _logger;
    private readonly IHubContext<StatusHub> _statusHub;
    private readonly IHubContext<AlarmesHub> _alarmesHub;
    private readonly IHubContext<AvisosHub> _avisosHub;
    private readonly IHubContext<IOsHub> _iosHub;
    private readonly IHubContext<VelocidadeHub> _velocidadeHub;
    private readonly IHubContext<DadosHub> _dadosHub;
    private readonly IHubContext<ContagemHub> _contagemHub;

    /**
     * Construtor do controller.
     * O ASP.NET Core usa Injeção de Dependência para fornecer automaticamente as instâncias
     * dos serviços registrados no Program.cs.
     * @param context A instância do DbContext para acesso ao banco de dados.
     * @param logger A instância para registro de logs.
     * @param statusHub O contexto do Hub SignalR para enviar mensagens de status.
     * ...e assim por diante para todos os outros hubs.
     */
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
        // Atribui as instâncias injetadas aos campos privados da classe.
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

    // ===================================================================================
    // 2. ENDPOINTS DA NOVA ARQUITETURA (SALVAR E ENVIAR)
    // ===================================================================================

    /**
    * <summary>
    * Recebe e processa um evento de STATUS vindo especificamente da ROTULADORA.
    * </summary>
    * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
    * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
    */
    [HttpPost("rotuladora/status")]
    public async Task<IActionResult> PostStatusRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // --- PASSO 1: Salvar no Banco de Dados ---
            var novoEvento = new EventoMaquina
            {
                /*
                Timestamp = DateTime.UtcNow,
                Origem = "Rotuladora",
                TipoEvento = "Status", 
                CodigoEvento = dadosRecebidos.Id,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
                */
                Timestamp = DateTime.UtcNow,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao

            };

            // Salva o novo evento na tabela EventosMaquina
            _context.EventosMaquina.Add(novoEvento);
            await _context.SaveChangesAsync();

            // --- PASSO 2: Enviar via SignalR para o Hub de Avisos ---
            // Note que estamos usando o _avisosHub que foi injetado no construtor
            await _statusHub.Clients.All.SendAsync("postStatus", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, novoEvento.Timestamp.ToString("o"));

            // Retorna sucesso para o Node-RED
            return Ok(new { message = "Status da Rotuladora salvo e enviado com sucesso." });
        }
        catch (Exception ex)
        {
            // Em caso de falha, registra o erro específico para este endpoint
            _logger.LogError(ex, "Falha ao processar status da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }
    }

    /**
     * <summary>
     * Recebe e processa um evento de ALARME vindo especificamente da ROTULADORA.
     * </summary>
     * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
     * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
     */
    [HttpPost("rotuladora/alarmes")]
    public async Task<IActionResult> PostAlarmeRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        // --- Plano A: Tenta executar as operações críticas e secundárias ---
        try
        {
            // --- PASSO 1 (Crítico): SALVAR NO BANCO DE DADOS ---

            // Cria uma nova instância do nosso modelo de banco de dados 'EventoMaquina'.
            var novoEvento = new EventoMaquina
            {
                /*
                // Gera um timestamp no momento em que o dado é recebido pelo servidor.
                Timestamp = DateTime.UtcNow,
                // Define o contexto 'Origem' e 'TipoEvento' com base na rota que foi chamada.
                Origem = "Rotuladora",
                TipoEvento = "Alarme",
                // Mapeia os dados recebidos do ApiModel para as colunas correspondentes.
                CodigoEvento = dadosRecebidos.Id,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
                */
                Timestamp = DateTime.UtcNow,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
            };

            // Adiciona o novo objeto ao 'contexto' de rastreamento do Entity Framework.
            _context.EventosMaquina.Add(novoEvento);
            // Efetiva a transação e salva permanentemente o novo registro no banco de dados.
            await _context.SaveChangesAsync();

            // --- PASSO 2 (Secundário): ENVIAR VIA SIGNALR ---

            // Envia os dados para todos os clientes conectados no Hub de alarmes.
            // O primeiro parâmetro "postAlarmes" é o nome do método que o JavaScript no front-end vai ouvir.
             await _alarmesHub.Clients.All.SendAsync("postAlarmes", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, novoEvento.Timestamp.ToString("o"));

            // Retorna uma resposta HTTP 200 OK para o Node-RED, confirmando o sucesso.
            return Ok(new { message = "Alarme da Rotuladora salvo e enviado com sucesso." });
        }
        // --- Plano B: Captura qualquer erro que tenha ocorrido no Plano A ---
        catch (Exception ex)
        {
            // Em caso de qualquer falha (no banco ou no SignalR), registra o erro detalhado para diagnóstico do desenvolvedor.
            _logger.LogError(ex, "Falha ao processar alarme da Rotuladora.");
            // Retorna um erro genérico HTTP 500 para o cliente que fez a requisição.
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }
    }

    /**
     * <summary>
     * Recebe e processa um evento de AVISO vindo especificamente da ROTULADORA.
     * </summary>
     * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
     * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
     */
    [HttpPost("rotuladora/avisos")]
    public async Task<IActionResult> PostAvisoRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // --- PASSO 1: Salvar no Banco de Dados ---
            var novoEvento = new EventoMaquina
            {
                /*
                Timestamp = DateTime.UtcNow,
                Origem = "Rotuladora",
                TipoEvento = "Aviso",
                CodigoEvento = dadosRecebidos.Id,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
                */
                Timestamp = DateTime.UtcNow,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
            };

            // Salva o novo evento na tabela EventosMaquina
            _context.EventosMaquina.Add(novoEvento);
            await _context.SaveChangesAsync();

            // --- PASSO 2: Enviar via SignalR para o Hub de Avisos ---
            // Note que estamos usando o _avisosHub que foi injetado no construtor
            await _avisosHub.Clients.All.SendAsync("postAvisos", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, novoEvento.Timestamp.ToString("o"));

            // Retorna sucesso para o Node-RED
            return Ok(new { message = "Aviso da Rotuladora salvo e enviado com sucesso." });
        }
        catch (Exception ex)
        {
            // Em caso de falha, registra o erro específico para este endpoint
            _logger.LogError(ex, "Falha ao processar aviso da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }
    }
    /**
    * <summary>
    * Recebe e processa um evento de IOs vindo especificamente da ROTULADORA.
    * </summary>
    * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
    * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
    */
    [HttpPost("rotuladora/IOs")]
    public async Task<IActionResult> PostIOsRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {

            /*
            // --- PASSO 1: Salvar no Banco de Dados ---
            var novoEvento = new EventoMaquina
            {
                Timestamp = DateTime.UtcNow,
                Origem = "Rotuladora",
                TipoEvento = "IOs",
                CodigoEvento = dadosRecebidos.Id,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
            };

            // Salva o novo evento na tabela EventosMaquina
            _context.EventosMaquina.Add(novoEvento);
            await _context.SaveChangesAsync();
            */

            // --- PASSO 2: Enviar via SignalR para o Hub de Avisos ---

            string timestamp = DateTime.UtcNow.ToString("o");

            // Note que estamos u
            // sando o _avisosHub que foi injetado no construtor
            await _iosHub.Clients.All.SendAsync("postIOs", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, timestamp);

            // Retorna sucesso para o Node-RED
            return Ok(new { message = "IOs da Rotuladora salvo e enviado com sucesso." });
        }
        catch (Exception ex)
        {
            // Em caso de falha, registra o erro específico para este endpoint
            _logger.LogError(ex, "Falha ao processar IOs da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }
    }

    /**
    * <summary>
    * Recebe e processa um evento de VELOCIDADE vindo especificamente da ROTULADORA.
    * </summary>
    * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
    * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
    */
    [HttpPost("rotuladora/velocidade")]
    public async Task<IActionResult> PostVelocidadeRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            /*
            var novoEvento = new EventoMaquina
            {
                Timestamp = DateTime.UtcNow,
                Origem = "Rotuladora",
                TipoEvento = "Velocidade",
                CodigoEvento = dadosRecebidos.Id,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
            };
            _context.EventosMaquina.Add(novoEvento);
         
            await _context.SaveChangesAsync();
            */

            if (double.TryParse(dadosRecebidos.Valor, out double velocidade))
            {
                var novoRegistro = new VelocidadeInstMaquina
                {

                    Timestamp = DateTime.UtcNow,
                    IdMaquina = dadosRecebidos.CodigoEvento,
                    Velocidade = velocidade


                };

                _context.VelocidadeInstMaquina.Add(novoRegistro);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Valor inválido recebido na velocidade: {Valor}", dadosRecebidos.Valor);
                return BadRequest("O valor de velocidade recebido não é numérico.");
            }


            string timestamp = DateTime.UtcNow.ToString("o");

            await _velocidadeHub.Clients.All.SendAsync("postVelocidade", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, timestamp);

            return Ok(new { message = "Velocidade da Rotuladora salva e enviada com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar dados de velocidade da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de dados da Rotuladora.");
        }
    }

    /**
    * <summary>
    * Recebe e processa um evento de CONTAGEM vindo especificamente da ROTULADORA.
    * </summary>
    * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
    * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
    */
    [HttpPost("rotuladora/contagem")]
    public async Task<IActionResult> PostContagemRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoEvento = new EventoMaquina
            {
                //Timestamp = DateTime.UtcNow,
                //Origem = "Rotuladora",
                //TipoEvento = "Contagem", 
                //CodigoEvento = dadosRecebidos.Id,
                //Valor = dadosRecebidos.Valor,
                //Informacao = dadosRecebidos.Informacao
                Timestamp = DateTime.UtcNow,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao

            };
            _context.EventosMaquina.Add(novoEvento);
            await _context.SaveChangesAsync();

            // Envia a notificação para o Hub correto (ContagemHub).
            await _contagemHub.Clients.All.SendAsync("postContagem", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, novoEvento.Timestamp.ToString("o"));

            return Ok(new { message = "Contagem da Rotuladora salva e enviada com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar contagem da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }
    }

    /**
      * <summary>
      * Recebe e processa um evento de CONTAGEM vindo especificamente da ROTULADORA.
      * </summary>
      * <param name="dadosRecebidos">Os dados genéricos enviados pelo Node-RED no formato ApiModel.</param>
      * <returns>Um resultado HTTP Ok ou um erro 500 em caso de falha.</returns>
      */
    [HttpPost("rotuladora/dados")]
    public async Task<IActionResult> PostDadosRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            var novoEvento = new EventoMaquina
            {
                /*
                Timestamp = DateTime.UtcNow,
                Origem = "Rotuladora",
                TipoEvento = "Dados",
                CodigoEvento = dadosRecebidos.Id,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
                */
                Timestamp = DateTime.UtcNow,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao
            };
            _context.EventosMaquina.Add(novoEvento);
            await _context.SaveChangesAsync();

            await _dadosHub.Clients.All.SendAsync("postContagem", dadosRecebidos.CodigoEvento, dadosRecebidos.Valor, dadosRecebidos.Informacao, novoEvento.Timestamp.ToString("o"));

            return Ok(new { message = "Contagem da Rotuladora salva e enviada com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar dados da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }
    }

    

}