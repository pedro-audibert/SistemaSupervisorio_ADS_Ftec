
using System.ComponentModel.DataAnnotations;

public class EventoMaquina
{
    [Key]
    public long Id { get; set; } // Identificador único de cada evento

    [Required]
    public DateTime Timestamp { get; set; } // QUANDO o evento ocorreu

    [Required]
    public string Origem { get; set; } // DE ONDE veio (Rotuladora, Enchedora...)

    [Required]
    public string TipoEvento { get; set; } // O QUE é (Alarme, Contagem...)

    [Required]
    public string CodigoEvento { get; set; } // QUAL evento específico (E-101, PecasBoas...)

    public string? Valor { get; set; } // O valor associado ao evento

    public string? Informacao { get; set; } // Um texto descritivo
}