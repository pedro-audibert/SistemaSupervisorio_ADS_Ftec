using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[HttpGet("alarmes/ultimos/{quantidade}")]
public async Task<IActionResult> GetUltimosAlarmes(int quantidade)
{
    try
    {
        var ultimosAlarmes = await _context.EventosMaquina
            .Where(e => e.TipoEvento == "Alarme")
            .OrderByDescending(e => e.Timestamp)
            .Take(quantidade)
            .Select(e => new {
                Id = e.CodigoEvento,
                Valor = e.Valor,
                Informacao = e.Informacao,
                Timestamp = e.Timestamp.ToString("o")
            })
            .ToListAsync();

        return Ok(ultimosAlarmes);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao buscar os últimos alarmes.");
        return StatusCode(500, "Erro ao buscar alarmes.");
    }
}
