using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using mmdba.Hubs;

public class TesteController : Controller
{
    private readonly IHubContext<IOHub> _hub;

    public TesteController(IHubContext<IOHub> hub)
    {
        _hub = hub;
    }

    public async Task<IActionResult> EnviarTeste()
    {
        // Simula atualização de um dado no painel de manutenção
        await _hub.Clients.All.SendAsync("AtualizarItem", "in0_1", true);
        return Content("Enviado!");
    }
}
