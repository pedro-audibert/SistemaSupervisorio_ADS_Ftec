// Models/Entidades/RegraNotificacao.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mmdba.Models; // Necessário para ApplicationUser

namespace mmdba.Models.Entidades
{
    /// <summary>
    /// Define qual tipo de evento (Alarme, Aviso, Status) deve ser notificado para qual usuário.
    /// </summary>
    public class RegraNotificacao
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Tipo de evento: "Alarme", "Aviso", "Status".
        /// </summary>
        [Required]
        [StringLength(50)]
        public string TipoEvento { get; set; }

        /// <summary>
        /// ID do usuário que receberá a notificação.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Propriedade de navegação para o usuário.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}