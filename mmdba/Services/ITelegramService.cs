namespace mmdba.Services
{
    // Interface para o nosso serviço de notificações do Telegram
    public interface ITelegramService
    {
        /// <summary>
        /// Envia uma mensagem para um Chat ID específico do Telegram.
        /// </summary>
        /// <param name="chatId">O ID do chat de destino (para quem vai a msg).</param>
        /// <param name="mensagem">A mensagem a ser enviada (pode conter HTML).</param>
        Task EnviarMensagemAsync(string chatId, string mensagem);
    }
}