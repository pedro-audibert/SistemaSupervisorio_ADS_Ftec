// Licenciado para a .NET Foundation sob um ou mais acordos.
// A .NET Foundation licencia este arquivo para você sob a licença MIT.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using mmdba.Models;

namespace mmdba.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;

        public DeletePersonalDataModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<DeletePersonalDataModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida em versões futuras.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
        /// e não é destinada ao uso direto no seu código. 
        /// Esta API pode mudar ou ser removida in versões futuras.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Esta API oferece suporte à infraestrutura da UI padrão do ASP.NET Core Identity 
            /// e não é destinada ao uso direto no seu código. 
            /// Esta API pode mudar ou ser removida em versões futuras.
            /// </summary>
            [Required(ErrorMessage = "A senha é obrigatória.")]
            [DataType(DataType.Password)]
            [Display(Name = "Senha atual")]
            public string Password { get; set; }
        }

        /// <summary>
        /// Indica se a senha é exigida para excluir a conta.
        /// </summary>
        public bool RequirePassword { get; set; }

        // Método GET: chamado quando o usuário acessa a página
        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        // Método POST: chamado quando o usuário confirma a exclusão
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Senha incorreta.");
                    return Page();
                }
            }

            var result = await _userManager.DeleteAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Ocorreu um erro inesperado ao excluir o usuário.");
            }

            await _signInManager.SignOutAsync();

            _logger.LogInformation("Usuário com ID '{UserId}' excluiu sua própria conta.", userId);

            return Redirect("~/");
        }
    }
}