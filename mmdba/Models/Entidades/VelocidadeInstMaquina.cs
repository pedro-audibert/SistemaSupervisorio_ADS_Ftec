using System;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models.Entidades
{
    public class VelocidadeInstMaquina
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public string IdMaquina { get; set; } = null!;

        [Required]
        public double Velocidade { get; set; }
    }
}
