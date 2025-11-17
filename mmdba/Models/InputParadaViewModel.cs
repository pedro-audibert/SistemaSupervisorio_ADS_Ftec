using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    /// <summary>
    /// ViewModel usado especificamente para o formulário
    /// no modal "Adicionar Parada Planejada".
    /// </summary>
    public class InputParadaViewModel
    {
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(100)]
        [Display(Name = "Descrição da Parada")]
        public string Descricao { get; set; } // Ex: "Almoço", "Limpeza"

        [Required(ErrorMessage = "A duração é obrigatória.")]
        [Range(1, 999, ErrorMessage = "A duração deve ser entre 1 e 999 minutos.")]
        [Display(Name = "Duração (em minutos)")]
        public int DuracaoMinutos { get; set; }

        // Este campo será preenchido pelo JavaScript (data-turnoid)
        [Required]
        public int TurnoId { get; set; }
    }
}