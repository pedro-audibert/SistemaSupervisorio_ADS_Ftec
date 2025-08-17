// Arquivo: Models/Entidades/ProducaoInstMaquina.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models.Entidades
{
    public class ProducaoInstMaquina
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string CodigoEvento { get; set; }

        [Required]
        public string Valor { get; set; }

        [Required]
        public string Informacao { get; set; }

        [Required]
        public string Origem { get; set; }

        [Required]
        public string TipoEvento { get; set; }

        public DateTime Timestamp { get; set; }
    }
}