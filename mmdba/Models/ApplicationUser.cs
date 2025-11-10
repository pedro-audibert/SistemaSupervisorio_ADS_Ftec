using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    public class ApplicationUser : IdentityUser
    {
        // 2. ADICIONE ESTA PROPRIEDADE
        /// <summary>
        /// Armazena o ID do Chat do Telegram deste utilizador.
        /// É para aqui que o bot enviará as notificações.
        /// </summary>
        [StringLength(50)]
        public string? TelegramChatId { get; set; }
    }
}