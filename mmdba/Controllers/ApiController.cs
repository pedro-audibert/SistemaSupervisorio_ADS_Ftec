/*
=========================================================================================================
ARQUIVO: Controllers/ApiController.cs
FUNÇÃO: Fornece endpoints da API para receber dados em tempo real da Rotuladora.
          Cada endpoint processa a requisição, persiste os dados no banco de dados, envia
          atualizações via SignalR e (FUNCIONALIDADE 7) dispara notificações no Telegram 
          se uma regra for correspondente.
=========================================================================================================
*/

#region NAMESPACES
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // Para FirstOrDefaultAsync
using Microsoft.Extensions.Logging;
using mmdba.Data;
using mmdba.Hubs;
using mmdba.Models;
using mmdba.Models.Entidades;
using mmdba.Services; // ADICIONADO para ITelegramService
using System;
using System.Threading.Tasks;
#endregion

[Route("api/mmdba/")]
[ApiController]
[ApiKey] // Atributo de segurança que valida a chave de API da requisição
public class ApiController : ControllerBase
{
    // --- PROPRIEDADES (SERVIÇOS INJETADOS) ---

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiController> _logger;
    private readonly IHubContext<StatusHub> _statusHub;
    private readonly IHubContext<AlarmesHub> _alarmesHub;
    private readonly IHubContext<AvisosHub> _avisosHub;
    private readonly IHubContext<IOsHub> _iosHub;
    private readonly IHubContext<VelocidadeHub> _velocidadeHub;
    private readonly IHubContext<DadosHub> _dadosHub;
    private readonly IHubContext<ContagemHub> _contagemHub;
    private readonly ITelegramService _telegramService; // ADICIONADO (Funcionalidade 7)                                        // ... (per Perto das outras propriedades privadas)
    private readonly IServiceScopeFactory _scopeFactory; // <-- ADICIONE ESTA LINHA


    // --- CONSTRUTOR ---

    /// <summary>
    /// Construtor do ApiController. A Injeção de Dependência fornece todos os serviços necessários.
    /// </summary>
    public ApiController(
        ApplicationDbContext context,
        ILogger<ApiController> logger,
        IHubContext<StatusHub> statusHub,
        IHubContext<AlarmesHub> alarmesHub,
        IHubContext<AvisosHub> avisosHub,
        IHubContext<IOsHub> iosHub,
        IHubContext<VelocidadeHub> velocidadeHub,
        IHubContext<ContagemHub> contagemHub,
        IHubContext<DadosHub> dadosHub,
        IServiceScopeFactory scopeFactory, // <-- ADICIONE ESTE PARÂMETRO
        ITelegramService telegramService) // Parâmetro do Telegram adicionado
    {
        // Atribuição dos serviços
        _context = context;
        _logger = logger;
        _statusHub = statusHub;
        _alarmesHub = alarmesHub;
        _avisosHub = avisosHub;
        _iosHub = iosHub;
        _velocidadeHub = velocidadeHub;
        _contagemHub = contagemHub;
        _dadosHub = dadosHub;
        _telegramService = telegramService;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Endpoint para receber Status da Rotuladora.
    /// </summary>
    [HttpPost("rotuladora/status")]
    public async Task<IActionResult> PostStatusRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // 1. Cria a entidade EventoMaquina
            var novoStatus = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow // Sempre usar UTC no servidor
            };

            // 2. Salva no Banco de Dados
            _context.EventosMaquina.Add(novoStatus);
            await _context.SaveChangesAsync();

            // 3. (FUNCIONALIDADE 7) Dispara a verificação de notificação (sem aguardar)
            _ = VerificarENotificarAsync(novoStatus);

            // 4. Envia para o SignalR
            await _statusHub.Clients.All.SendAsync("postStatus", novoStatus);

            return Ok(new { message = "Status da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar status da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de status da Rotualdora.");
        }
    }

    /// <summary>
    /// Endpoint para receber Alarmes da Rotuladora.
    /// </summary>
    [HttpPost("rotuladora/alarmes")]
    public async Task<IActionResult> PostAlarmeRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // 1. Cria a entidade
            var novoAlarme = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            // 2. Salva no Banco de Dados
            _context.EventosMaquina.Add(novoAlarme);
            await _context.SaveChangesAsync();

            // 3. (FUNCIONALIDADE 7) Dispara a verificação de notificação (sem aguardar)
            _ = VerificarENotificarAsync(novoAlarme);

            // 4. Envia para o SignalR
            await _alarmesHub.Clients.All.SendAsync("postAlarmes", novoAlarme);

            return Ok(new { message = "Alarme da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar Alarme da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Alarme da Rotualadora.");
        }
    }

    /// <summary>
    /// Endpoint para receber Avisos da Rotuladora.
    /// </summary>
    [HttpPost("rotuladora/avisos")]
    public async Task<IActionResult> PostAvisoRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // 1. Cria a entidade
            var novoAviso = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            // 2. Salva no Banco de Dados
            _context.EventosMaquina.Add(novoAviso);
            await _context.SaveChangesAsync();

            // 3. (FUNCIONALIDADE 7) Dispara a verificação de notificação (sem aguardar)
            _ = VerificarENotificarAsync(novoAviso);

            // 4. Envia para o SignalR
            await _avisosHub.Clients.All.SendAsync("postAvisos", (novoAviso));

            return Ok(new { message = "Aviso da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar aviso da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Aviso da Rotuladora.");
        }
    }

    /// <summary>
    /// Endpoint para receber IOs (Entradas/Saídas) da Rotuladora.
    /// OBS: Este endpoint não persiste no banco, apenas atualiza a tela via SignalR.
    /// </summary>
    [HttpPost("rotuladora/IOs")]
    public async Task<IActionResult> PostIOsRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // Cria um objeto temporário apenas para o SignalR
            var novoIOs = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            // Envia para o SignalR
            await _iosHub.Clients.All.SendAsync("postIOs", novoIOs);
            return Ok(new { message = "Evento de IO da máquina recebido com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar IOs da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de IOs da Rotuladora.");
        }
    }

    /// <summary>
    /// Endpoint para receber Velocidade da Rotuladora.
    /// Persiste na tabela 'VelocidadeInstMaquina'.
    /// </summary>
    [HttpPost("rotuladora/velocidade")]
    public async Task<IActionResult> PostVelocidadeRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // 1. Cria a entidade (tabela específica)
            var novaVelocidade = new VelocidadeInstMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            // 2. Salva no Banco de Dados
            _context.VelocidadeInstMaquina.Add(novaVelocidade);
            await _context.SaveChangesAsync();

            // 3. Envia para o SignalR
            await _velocidadeHub.Clients.All.SendAsync("postVelocidade", novaVelocidade);

            return Ok(new { message = "Velocidade da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar dados de velocidade da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de dados da Rotuladora.");
        }
    }

    /// <summary>
    /// Endpoint para receber Contagem de Produção da Rotuladora.
    /// Persiste na tabela 'ProducaoInstMaquina'.
    /// </summary>
    [HttpPost("rotuladora/contagem")]
    public async Task<IActionResult> PostContagemRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // 1. Cria a entidade (tabela específica)
            var novaContagem = new ProducaoInstMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            // 2. Salva no Banco de Dados
            _context.ProducaoInstMaquina.Add(novaContagem);
            await _context.SaveChangesAsync();

            // 3. Envia para o SignalR
            await _contagemHub.Clients.All.SendAsync("postContagem", novaContagem);

            return Ok(new { message = "Contagem de produção da máquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar contagem da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Contagem da Rotuladora.");
        }
    }

    /// <summary>
    /// Endpoint para receber Dados Genéricos da Rotuladora.
    /// </summary>
    [HttpPost("rotuladora/dados")]
    public async Task<IActionResult> PostDadosRotuladora([FromBody] ApiModel dadosRecebidos)
    {
        try
        {
            // 1. Cria a entidade
            var novoDado = new EventoMaquina
            {
                CodigoEvento = dadosRecebidos.CodigoEvento,
                Valor = dadosRecebidos.Valor,
                Informacao = dadosRecebidos.Informacao,
                Origem = dadosRecebidos.Origem,
                TipoEvento = dadosRecebidos.TipoEvento,
                Timestamp = DateTime.UtcNow
            };

            // 2. Salva no Banco de Dados
            _context.EventosMaquina.Add(novoDado);
            await _context.SaveChangesAsync();

            // 3. (FUNCIONALIDADE 7) Dispara a verificação de notificação (sem aguardar)
            _ = VerificarENotificarAsync(novoDado);

            // 4. Envia para o SignalR
            await _dadosHub.Clients.All.SendAsync("postDados", (novoDado));

            return Ok(new { message = "Dado da maquina recebido e salvo com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar dados da Rotuladora.");
            return StatusCode(500, "Erro interno ao processar a requisição de Dados da Rotuladora.");
        }
    }

    /*
    ====================================================================================
    MÉTODO PRIVADO: VerificarENotificarAsync (Versão Final - Dinâmica)
    FUNÇÃO: (FUNCIONALIDADE 7)
            Verifica o evento contra as regras, encontra os utilizadores
            associados a essas regras, e envia a notificação para o
            'TelegramChatId' específico de cada utilizador.
    ====================================================================================
    */
    /// <summary>
    /// Método auxiliar que verifica e envia notificações via Telegram
    /// com base nas Regras de Notificação cadastradas.
    /// </summary>
    /// <param name="evento">O EventoMaquina que acabou de ser salvo.</param>
    private async Task VerificarENotificarAsync(EventoMaquina evento)
    {
        // 1. CRIAR UM NOVO ESCOPO DE SERVIÇO (para evitar Race Condition)
        using (var scope = _scopeFactory.CreateScope())
        {
            try
            {
                // 2. PEDIR OS SERVIÇOS (DbContext e Logger) DO NOVO ESCOPO
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApiController>>();

                // 3. Encontrar todas as regras para este TipoEvento,
                //    e INCLUIR (Include) a informação do Utilizador (User)
                var regrasParaEsteEvento = await dbContext.RegrasNotificacao
                    .Where(r => r.TipoEvento == evento.TipoEvento)
                    .Include(r => r.User) // <-- Isto faz o JOIN com a tabela AspNetUsers
                    .ToListAsync();

                // 4. Se nenhuma regra foi encontrada, não faz nada
                if (regrasParaEsteEvento == null || !regrasParaEsteEvento.Any())
                {
                    return;
                }

                // 5. Formata a mensagem (NOVO FORMATO PROFISSIONAL)
                string valorLimpo = evento.Valor ?? "N/A";
                string infoLimpa = evento.Informacao ?? "N/A";

                string mensagemTelegram =
                    $"🏭 <b>MMDBA | SISTEMA SUPERVISÓRIO</b>\n" +
                    $"--------------------------------------\n" +
                    $"<b>Evento:</b>   <code>{evento.TipoEvento}</code>\n" +
                    $"<b>Máquina:</b>  <code>{evento.Origem}</code>\n" +
                    $"<b>Código:</b>   <code>{evento.CodigoEvento}</code>\n" +
                    $"<b>Valor:</b>    <code>{valorLimpo}</code>\n" +
                    $"--------------------------------------\n" +
                    $"ℹ️ <b>Info:</b> <i>{infoLimpa}</i>";

                // 6. Itera sobre cada regra encontrada
                foreach (var regra in regrasParaEsteEvento)
                {
                    // 7. Verifica se o utilizador da regra existe e se tem um ChatId configurado
                    if (regra.User != null && !string.IsNullOrEmpty(regra.User.TelegramChatId))
                    {
                        // 8. CHAMA O MÉTODO CORRETAMENTE (com 2 parâmetros)
                        //    (O _telegramService é Singleton, podemos usá-lo aqui)
                        await _telegramService.EnviarMensagemAsync(regra.User.TelegramChatId, mensagemTelegram);
                        logger.LogInformation($"Notificação enviada para o utilizador '{regra.User.UserName}' (ChatId: {regra.User.TelegramChatId}).");
                    }
                    else
                    {
                        logger.LogWarning($"Regra ID {regra.Id} acionada, mas o utilizador '{regra.UserId}' não tem um TelegramChatId configurado.");
                    }
                }
            }
            catch (Exception ex)
            {
                // O ILogger (do scope) é seguro de usar aqui
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApiController>>();
                logger.LogError(ex, "Falha na tarefa de fundo (VerificarENotificarAsync - Dinâmico).");
            }
        }
    }
}