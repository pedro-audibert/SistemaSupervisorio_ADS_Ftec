using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mmdba.Models.Entidades
{
    public class ParadaPlanejada
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Descricao { get; set; } // Ex: "Almoço", "Manutenção Preventiva"

        [Required]
        public int DuracaoMinutos { get; set; }

        // Chave Estrangeira para o Turno
        [Required]
        public int TurnoId { get; set; }

        // Propriedade de Navegação
        [ForeignKey("TurnoId")]
        public virtual Turno Turno { get; set; }
    }
}