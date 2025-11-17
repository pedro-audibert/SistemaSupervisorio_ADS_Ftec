using System;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    /// <summary>
    /// Modelo de dados para receber o lançamento manual de refugo via API.
    /// </summary>
    public class RefugoManualApiModel
    {
        [Required]
        public string MaquinaId { get; set; }

        [Required]
        [Range(-2000000000, int.MaxValue, ErrorMessage = "Valor inválido para Quantidade.")] public int Quantidade { get; set; }

        // O Timestamp é opcional, mas útil para registrar a hora exata do lançamento.
        public DateTime? Timestamp { get; set; }
    }
}