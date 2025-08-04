// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using mmdba.Models;

namespace mmdba.Areas.Identity.Pages.Account
{
    // Esta classe controla a lógica do lado do servidor para a página de Login.
    public class LoginModel : PageModel
    {
        // Serviços injetados pelo ASP.NET Core para gerenciar o processo de login e registrar logs.
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // [BindProperty] conecta esta propriedade 'Input' aos campos do formulário na página .cshtml.
        [BindProperty]
        public InputModel Input { get; set; }

        // Lista de provedores de login externos (ex: Google, Facebook), se houver.
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // URL para a qual o usuário será redirecionado após o login bem-sucedido.
        public string ReturnUrl { get; set; }

        // [TempData] armazena mensagens de erro que persistem mesmo após um redirecionamento.
        [TempData]
        public string ErrorMessage { get; set; }

        // Classe interna que define a estrutura e as regras de validação para os campos do formulário de login.
        public class InputModel
        {
            [Required(ErrorMessage = "O campo Email é obrigatório.")]
            [EmailAddress(ErrorMessage = "O campo Email não é um endereço de e-mail válido.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "O campo Senha é obrigatório.")]
            [DataType(DataType.Password)]
            [Display(Name = "Senha")]
            public string Password { get; set; }

            [Display(Name = "Lembrar de mim?")]
            public bool RememberMe { get; set; }
        }

        // Método executado quando a página é carregada via GET (quando o usuário acessa a página pela primeira vez).
        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Limpa qualquer cookie externo existente para garantir um processo de login limpo.
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        // Método executado quando o formulário é enviado via POST (quando o usuário clica em "Entrar").
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Verifica se os dados do formulário (Input) são válidos com base nas anotações (ex: [Required]).
            if (ModelState.IsValid)
            {
                // Tenta fazer o login do usuário com a senha fornecida.
                // lockoutOnFailure: false -> errar a senha não bloqueará a conta.
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuário logado com sucesso.");
                    return LocalRedirect(returnUrl); // Redireciona para a página de origem ou para a Home.
                }
                if (result.RequiresTwoFactor)
                {
                    // Se o usuário tiver 2FA ativado, redireciona para a página de login de 2FA.
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Conta de usuário bloqueada.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    // Se nenhuma das condições acima for atendida, significa que o email ou a senha estão incorretos.
                    // AQUI ESTÁ A TRADUÇÃO:
                    ModelState.AddModelError(string.Empty, "Tentativa de login inválida.");
                    return Page(); // Recarrega a página de login para exibir o erro.
                }
            }

            // Se o ModelState não for válido (ex: campos em branco), recarrega a página.
            return Page();
        }
    }
}