/*
================================================================================================
ARQUIVO: HomeController.cs
FUNÇÃO:  Controlador principal MVC (Model-View-Controller) responsável por servir as
         páginas (Views) da aplicação. Funcionalidades principais:
         1. Servir a página 'Index' como portal de seleção de máquinas.
         2. Servir as Views de painéis de máquinas (Rotuladora, Enchedora, etc.).
         3. Servir páginas públicas e utilitárias (Privacidade, Erro).
================================================================================================
*/

#region NAMESPACES
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mmdba.Models;
using System.Diagnostics;
#endregion

namespace mmdba.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Página principal do portal de seleção de máquinas.
        /// </summary>
        public IActionResult Index()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o portal de seleção de máquinas.", User.Identity?.Name);
            return View();
        }

        /// <summary>
        /// Painel de supervisão em tempo real.
        /// </summary>
        public IActionResult PainelSupervisao()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o Painel de Supervisão.", User.Identity?.Name);
            return View();
        }

        /// <summary>
        /// Painel de manutenção da Rotuladora BOPP.
        /// </summary>
        public IActionResult PainelManutencao()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o Painel de Manutenção da Rotuladora BOPP.", User.Identity?.Name);
            return View();
        }

        /// <summary>
        /// Painel de alarmes da Enchedora.
        /// </summary>
        public IActionResult PainelAlarmes()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o Painel de Alarmes da Enchedora.", User.Identity?.Name);
            return View();
        }

        /// <summary>
        /// Página de política de privacidade (pública).
        /// </summary>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            _logger.LogInformation("Visitante acessou a página de Privacidade.");
            return View();
        }

        /// <summary>
        /// Página de erro genérica (pública).
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Página de erro exibida. RequestId: {RequestId}", requestId);
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
