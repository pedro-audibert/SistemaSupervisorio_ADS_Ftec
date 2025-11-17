using mmdba.Models.Entidades; // Para aceder à classe Turno
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    public class ParametrosOEEViewModel
    {
        [Display(Name = "Velocidade Ideal (Garrafas/Hora)")]
        [Range(1, 99999, ErrorMessage = "O valor deve ser maior que 0.")]
        public int VelocidadeIdealPorHora { get; set; }

        // --- ADICIONE ESTAS LINHAS ABAIXO ---
        [Display(Name = "Habilitar Lançamento Manual de Refugo")]
        public bool HabilitarRefugoManual { get; set; }
        // --- FIM DA ADIÇÃO ---
        public int TaxaAtualizacaoMinutos { get; set; }
        public List<Turno> Turnos { get; set; }

        // REMOVIDO: InputTurno (causa prefixo duplicado no model binding)
        // O InputTurnoViewModel é usado diretamente na action do Controller

        public ParametrosOEEViewModel()
        {
            Turnos = new List<Turno>();
            // (O 'HabilitarRefugoManual' sendo um 'bool',
            //  começará automaticamente como 'false', o que é perfeito.)
        }
    }
}