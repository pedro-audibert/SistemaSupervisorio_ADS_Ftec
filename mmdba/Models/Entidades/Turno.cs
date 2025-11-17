using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models.Entidades
{
    public class Turno
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MaquinaId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nome { get; set; } 

        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }

        // Propriedade de Navegação: Um Turno pode ter várias Paradas Planejadas
        public virtual ICollection<ParadaPlanejada> ParadasPlanejadas { get; set; }
    }
}