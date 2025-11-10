using mmdba.Models; // Para TelegramSettings
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions; // Bom para capturar erros específicos da API
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace mmdba.Services
{
    /// <summary>
    /// Implementa a interface ITelegramService.
    /// Esta classe é responsável por se autenticar com o BotToken e enviar a mensagem.
    /// </summary>
    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramService> _logger;
        // O campo 'private readonly string _chatId;' foi REMOVIDO.
        // O serviço já não precisa de saber o seu ChatId pessoal.

        /// <summary>
        /// Construtor que recebe as configurações (BotToken) e o Logger.
        /// </summary>
        public TelegramService(IOptions<TelegramSettings> settings, ILogger<TelegramService> logger)
        {
            _logger = logger;

            // A validação do BotToken continua essencial
            if (string.IsNullOrEmpty(settings.Value.BotToken))
            {
                _logger.LogCritical("TOKEN do Bot Telegram não configurado no 'User Secrets'.");
                throw new ArgumentNullException(nameof(settings.Value.BotToken), "Token do Bot não pode ser nulo.");
            }

            // A validação do ChatId das configurações foi REMOVIDA.

            // O cliente do Bot é criado com o Token (o Remetente)
            _botClient = new TelegramBotClient(settings.Value.BotToken);
        }

        /// <summary>
        /// Envia uma mensagem para um Chat ID específico do Telegram.
        /// </summary>
        /// <param name="chatId">O ID do chat de destino (para quem vai a msg).</param>
        /// <param name="mensagem">A mensagem a ser enviada (pode conter HTML).</param>
        public async Task EnviarMensagemAsync(string chatId, string mensagem)
        {
            // Verificação de segurança para a mensagem
            if (string.IsNullOrEmpty(mensagem))
            {
                _logger.LogWarning("Tentativa de enviar mensagem vazia para o Telegram.");
                return;
            }

            // Verificação de segurança para o 'chatId' recebido
            if (string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Tentativa de enviar mensagem para um ChatId nulo ou vazio.");
                return;
            }

            try
            {
                // Usa o 'chatId' recebido como parâmetro (o Destinatário)
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: mensagem,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                );

                _logger.LogInformation($"Mensagem enviada com sucesso para o Telegram (ChatId: {chatId}).");
            }
            catch (ApiRequestException apiEx)
            {
                // Erro comum: O utilizador bloqueou o bot, ou o ChatId está errado
                _logger.LogError(apiEx, $"Erro da API do Telegram ao enviar para (ChatId: {chatId}). Mensagem: {apiEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro genérico ao enviar mensagem para o Telegram (ChatId: {chatId}).");
            }
        }
    }
}