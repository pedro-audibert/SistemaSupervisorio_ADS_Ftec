using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    /// <summary>
    /// ViewModel usado especificamente para o formulário
    /// no modal "Adicionar Novo Turno".
    /// </summary>
    public class InputTurnoViewModel
    {
        [Required(ErrorMessage = "O nome do turno é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome do Turno")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A hora de início é obrigatória.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de Início")]
        public string HoraInicio { get; set; }

        [Required(ErrorMessage = "A hora de fim é obrigatória.")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de Fim")]
        public string HoraFim { get; set; }
    }
}