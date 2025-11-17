using System.ComponentModel.DataAnnotations;

namespace mmdba.Models.Entidades
{
    public class OeeParametrosMaquina
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MaquinaId { get; set; }

        public int VelocidadeIdealPorHora { get; set; }

        public bool HabilitarRefugoManual { get; set; }

        public int TaxaAtualizacaoMinutos { get; set; } = 10;
    }
}