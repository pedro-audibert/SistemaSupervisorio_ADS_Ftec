/*
================================================================================================
ARQUIVO: HomeController.cs (VERSÃO FINAL COM DOCUMENTAÇÃO ATUALIZADA)
FUNÇÃO:  Controlador principal MVC (Model-View-Controller) responsável por servir as
         páginas (Views) da aplicação. Com a nova arquitetura, sua função principal é:
         1. Servir a página 'Index' como um portal para a seleção de máquinas.
         2. Servir as Views específicas de cada painel de máquina (Rotuladora, etc.).
         3. Servir páginas públicas e de utilidade (Privacidade, Erro).
================================================================================================
*/

#region NAMESPACES IMPORTADOS
using Microsoft.AspNetCore.Authorization; // Namespace que contém os atributos [Authorize] e [AllowAnonymous].
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mmdba.Models;
using System.Diagnostics;
using mmdba.Data; // <<< ADICIONADO: Para acessar o ApplicationDbContext
using System.Linq; // <<< ADICIONADO: Para usar os métodos de consulta (Where, OrderBy, etc.)
using System.Threading.Tasks; // <<< ADICIONADO: Para métodos assíncronos
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

        public IActionResult Index()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o portal de seleção de máquinas (Index).", User.Identity?.Name);
            return View();
        }
        public IActionResult PainelSupervisao()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o portal de seleção de máquinas (Index).", User.Identity?.Name);
            return View();
        }

        public IActionResult PainelManutencao()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o painel da Rotuladora BOPP.", User.Identity?.Name);
            return View();
        }

        public IActionResult PainelAlarmes()
        {
            _logger.LogInformation("Usuário '{UserName}' acessou o painel da Enchedora.", User.Identity?.Name);
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            _logger.LogInformation("Visitante acessou a página de Privacidade.");
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Página de erro genérica foi exibida. RequestId: {RequestId}", Activity.Current?.Id);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}