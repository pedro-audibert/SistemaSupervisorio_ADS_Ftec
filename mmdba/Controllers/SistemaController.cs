/*
================================================================================================
ARQUIVO: SistemaController.cs
FUNÇÃO:  Controlador MVC para a área de Administração do Sistema (Gestão de Usuários e Regras).
================================================================================================
*/

#region NAMESPACES
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mmdba.Data; // Necessário para ApplicationDbContext
using mmdba.Models;
using mmdba.Models.Entidades; // Necessário para RegraNotificacao
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic; // Necessário para List<T>
#endregion

namespace mmdba.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SistemaController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // Construtor com injeção de dependência do Identity e do DbContext.
        public SistemaController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Action para a página de Gestão de Usuários.
        [HttpGet]
        public async Task<IActionResult> Usuarios()
        {
            var viewModel = new GestaoUsuariosViewModel();
            await PopulaListaUsuarios(viewModel); // Chama o método auxiliar modificado
            ViewData["ActivePage"] = "Usuarios"; // Marca esta aba como ativa
            return View(viewModel);
        }

        // Action para a página de Regras de Notificação.
        [HttpGet]
        public async Task<IActionResult> RegrasNotificacao()
        {
            var viewModel = new RegrasNotificacaoViewModel();
            viewModel.UsuariosComPermissao = await PopulaListaUsuarios(); // Chama o método auxiliar modificado
            ViewData["ActivePage"] = "RegrasNotificacao"; // Marca esta aba como ativa
            return View(viewModel);
        }

        // ===== CRIAÇÃO DE USUÁRIO (AJAX) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarUsuarioAjax([Bind("InputNovoUsuario")] GestaoUsuariosViewModel viewModel)
        {
            var input = viewModel.InputNovoUsuario;

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = input.NovoEmail, Email = input.NovoEmail, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, input.NovoPassword);

                if (result.Succeeded)
                {
                    if (input.NovoIsAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                    }

                    return Json(new { success = true });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("InputNovoUsuario.NovoEmail", error.Description);
                }
            }

            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

            return Json(new { success = false, errors = errors });
        }

        // ===== REMOÇÃO DE USUÁRIO (AJAX) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverUsuarioAjax(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "ID do usuário não fornecido." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuário não encontrado." });
            }

            var adminLogadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == adminLogadoId)
            {
                return Json(new { success = false, message = "Não pode remover a sua própria conta de Administrador." });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Erro ao remover o usuário." });
        }

        // ===== OBTER DADOS DO USUÁRIO PARA EDIÇÃO (AJAX) =====
        [HttpGet]
        public async Task<IActionResult> ObterDadosUsuario(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuário não encontrado." });
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            var dadosUsuario = new
            {
                userId = user.Id,
                email = user.Email,
                isAdmin = isAdmin
            };

            return Json(new { success = true, usuario = dadosUsuario });
        }

        // ===== EDITAR PERMISSÕES DO USUÁRIO (AJAX) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuarioAjax(string editarUserId, bool editarIsAdmin)
        {
            if (string.IsNullOrEmpty(editarUserId))
            {
                return Json(new { success = false, message = "ID do usuário não fornecido." });
            }

            var user = await _userManager.FindByIdAsync(editarUserId);
            if (user == null)
            {
                return Json(new { success = false, message = "Usuário não encontrado." });
            }

            // Impede que o admin remova a própria permissão
            var adminLogadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == adminLogadoId && !editarIsAdmin)
            {
                return Json(new { success = false, message = "Não pode remover a sua própria permissão de Administrador." });
            }

            // Atualiza a permissão
            var isCurrentlyAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (editarIsAdmin && !isCurrentlyAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else if (!editarIsAdmin && isCurrentlyAdmin)
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }

            return Json(new { success = true });
        }


        // =======================================================
        // FUNÇÕES DE REGRAS DE NOTIFICAÇÃO (AJAX)
        // =======================================================

        // Obtém as regras de notificação ativas para um usuário específico.
        [HttpGet]
        public async Task<IActionResult> ObterRegrasUsuario(string userId)
        {
            var regras = await _context.RegrasNotificacao
                                        .Where(r => r.UserId == userId)
                                        .Select(r => r.TipoEvento)
                                        .ToListAsync();

            // Retorna um Dictionary com os tipos de eventos e seu status (true/false)
            var statusRegras = new Dictionary<string, bool>
            {
                { "Alarme", regras.Contains("Alarme") },
                { "Aviso", regras.Contains("Aviso") },
                { "Status", regras.Contains("Status") }
            };

            return Json(new { success = true, regras = statusRegras });
        }

        // Salva/remove uma regra de notificação (acionado por checkbox no frontend).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarRegrasUsuario(string userId, string tipoEvento, bool ativo)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tipoEvento))
            {
                return Json(new { success = false, message = "Parâmetros inválidos." });
            }

            var regraExistente = await _context.RegrasNotificacao
                                                .FirstOrDefaultAsync(r => r.UserId == userId && r.TipoEvento == tipoEvento);

            if (ativo && regraExistente == null)
            {
                // Adiciona a regra
                _context.RegrasNotificacao.Add(new RegraNotificacao { UserId = userId, TipoEvento = tipoEvento });
                await _context.SaveChangesAsync();
            }
            else if (!ativo && regraExistente != null)
            {
                // Remove a regra
                _context.RegrasNotificacao.Remove(regraExistente);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }


        // Função auxiliar para popular a lista de usuários e suas permissões.
        private async Task<List<UsuarioInfoViewModel>> PopulaListaUsuarios(GestaoUsuariosViewModel viewModel = null)
        {
            var lista = new List<UsuarioInfoViewModel>();

            // Aqui estamos a ler da tabela AspNetUsers (ApplicationUser)
            var usuariosDoBanco = await _userManager.Users.ToListAsync();

            foreach (var user in usuariosDoBanco)
            {
                lista.Add(new UsuarioInfoViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),

                    // --- LINHA ADICIONADA ---
                    // Preenche a propriedade TelegramChatId (que vem de ApplicationUser)
                    TelegramChatId = user.TelegramChatId
                });
            }
            // Se o viewModel foi passado (usado na Action Usuarios), preenche-o.
            if (viewModel != null)
            {
                viewModel.Usuarios = lista;
            }
            return lista;
        }
    }
}