namespace mmdba.Models
{
    // Esta classe vai 'carregar' as configurações do secrets.json
    public class TelegramSettings
    {
        public string BotToken { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
    }
}