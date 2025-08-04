/*
================================================================================================
ARQUIVO: HomeController.cs (VERS�O FINAL COM DOCUMENTA��O ATUALIZADA)
FUN��O:  Controlador principal MVC (Model-View-Controller) respons�vel por servir as
         p�ginas (Views) da aplica��o. Com a nova arquitetura, sua fun��o principal �:
         1. Servir a p�gina 'Index' como um portal para a sele��o de m�quinas.
         2. Servir as Views espec�ficas de cada painel de m�quina (Rotuladora, etc.).
         3. Servir p�ginas p�blicas e de utilidade (Privacidade, Erro).
================================================================================================
*/

#region NAMESPACES IMPORTADOS
using Microsoft.AspNetCore.Authorization; // Namespace que cont�m os atributos [Authorize] e [AllowAnonymous].
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mmdba.Models;
using System.Diagnostics;
using mmdba.Data; // <<< ADICIONADO: Para acessar o ApplicationDbContext
using System.Linq; // <<< ADICIONADO: Para usar os m�todos de consulta (Where, OrderBy, etc.)
using System.Threading.Tasks; // <<< ADICIONADO: Para m�todos ass�ncronos
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
            _logger.LogInformation("Usu�rio '{UserName}' acessou o portal de sele��o de m�quinas (Index).", User.Identity?.Name);
            return View();
        }
        public IActionResult PainelSupervisao()
        {
            _logger.LogInformation("Usu�rio '{UserName}' acessou o portal de sele��o de m�quinas (Index).", User.Identity?.Name);
            return View();
        }

        public IActionResult PainelManutencao()
        {
            _logger.LogInformation("Usu�rio '{UserName}' acessou o painel da Rotuladora BOPP.", User.Identity?.Name);
            return View();
        }

        public IActionResult PainelAlarmes()
        {
            _logger.LogInformation("Usu�rio '{UserName}' acessou o painel da Enchedora.", User.Identity?.Name);
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            _logger.LogInformation("Visitante acessou a p�gina de Privacidade.");
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("P�gina de erro gen�rica foi exibida. RequestId: {RequestId}", Activity.Current?.Id);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}