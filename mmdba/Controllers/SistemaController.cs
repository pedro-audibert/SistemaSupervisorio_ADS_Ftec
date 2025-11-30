/*
================================================================================================
ARQUIVO: SistemaController.cs
FUNÇÃO:  Controlador MVC para a área de Administração (Gestão de Usuários, Regras e Parâmetros OEE).
================================================================================================
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mmdba.Data;
using mmdba.Models;
using mmdba.Models.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace mmdba.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SistemaController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // Construtor com injeção de dependência
        public SistemaController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        #region VIEWS (GET)

        // Action para a página de Gestão de Usuários.
        [HttpGet]
        public async Task<IActionResult> Usuarios()
        {
            var viewModel = new GestaoUsuariosViewModel();
            await PopulaListaUsuarios(viewModel);
            ViewData["ActivePage"] = "Usuarios";
            return View(viewModel);
        }

        // Action para a página de Regras de Notificação.
        [HttpGet]
        public async Task<IActionResult> RegrasNotificacao()
        {
            var viewModel = new RegrasNotificacaoViewModel();
            viewModel.UsuariosComPermissao = await PopulaListaUsuarios();
            ViewData["ActivePage"] = "RegrasNotificacao";
            return View(viewModel);
        }

        // Action para a página de Parâmetros de OEE.
        [HttpGet]
        public async Task<IActionResult> ParametrosOEE()
        {
            ViewData["ActivePage"] = "ParametrosOEE";

            // 1. Obter a máquina selecionada da Sessão
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");

            if (string.IsNullOrEmpty(maquinaId))
            {
                return RedirectToAction("Index", "Home");
            }

            // 2. Criar o ViewModel
            var viewModel = new ParametrosOEEViewModel();

            // 3. Carregar Parâmetros (Velocidade, Refugo Manual, Taxa de Atualização)
            var parametros = await _context.OeeParametrosMaquina
                                            .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

            if (parametros != null)
            {
                viewModel.VelocidadeIdealPorHora = parametros.VelocidadeIdealPorHora;
                viewModel.HabilitarRefugoManual = parametros.HabilitarRefugoManual;
                // Carrega a taxa salva para exibir no input da tela
                viewModel.TaxaAtualizacaoMinutos = parametros.TaxaAtualizacaoMinutos;
            }

            // 4. Carregar Turnos e Paradas Planejadas
            viewModel.Turnos = await _context.Turnos
                                        .Where(t => t.MaquinaId == maquinaId)
                                        .Include(t => t.ParadasPlanejadas)
                                        .OrderBy(t => t.HoraInicio)
                                        .ToListAsync();

            return View(viewModel);
        }

        #endregion

        #region PARÂMETROS OEE (POST/AJAX)

        /// <summary>
        /// Salva a Velocidade Ideal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SalvarVelocidadeOEE(ParametrosOEEViewModel model)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                if (model.VelocidadeIdealPorHora <= 0)
                {
                    return Json(new { success = false, message = "A velocidade ideal deve ser um número maior que 0." });
                }

                var parametros = await _context.OeeParametrosMaquina
                                                .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

                if (parametros == null)
                {
                    parametros = new OeeParametrosMaquina
                    {
                        MaquinaId = maquinaId,
                        VelocidadeIdealPorHora = model.VelocidadeIdealPorHora
                    };
                    _context.OeeParametrosMaquina.Add(parametros);
                }
                else
                {
                    parametros.VelocidadeIdealPorHora = model.VelocidadeIdealPorHora;
                    _context.OeeParametrosMaquina.Update(parametros);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao salvar no banco de dados." });
            }
        }

        /// <summary>
        /// Salva a configuração de Qualidade (Habilitar Refugo Manual).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SalvarConfigQualidade(bool HabilitarRefugoManual)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                var parametros = await _context.OeeParametrosMaquina
                                            .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

                if (parametros == null)
                {
                    parametros = new OeeParametrosMaquina
                    {
                        MaquinaId = maquinaId
                    };
                    _context.OeeParametrosMaquina.Add(parametros);
                }

                parametros.HabilitarRefugoManual = HabilitarRefugoManual;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao salvar a configuração." });
            }
        }

        /// <summary>
        /// Action (POST) que salva a Taxa de Atualização Automática via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SalvarTaxaAtualizacaoOEE([FromBody] int taxaAtualizacao)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                if (taxaAtualizacao <= 0)
                {
                    return Json(new { success = false, message = "A taxa de atualização deve ser maior que 0." });
                }

                var parametros = await _context.OeeParametrosMaquina
                                            .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

                if (parametros == null)
                {
                    parametros = new OeeParametrosMaquina
                    {
                        MaquinaId = maquinaId,
                        TaxaAtualizacaoMinutos = taxaAtualizacao
                    };
                    _context.OeeParametrosMaquina.Add(parametros);
                }
                else
                {
                    parametros.TaxaAtualizacaoMinutos = taxaAtualizacao;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Taxa de atualização salva com sucesso." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao salvar a taxa." });
            }
        }

        /// <summary>
        /// Adiciona um novo Turno via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AdicionarTurnoOEE([FromForm(Name = "InputTurno")] InputTurnoViewModel model)
        {
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
            if (string.IsNullOrEmpty(maquinaId))
            {
                return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var novoTurno = new Turno
                    {
                        MaquinaId = maquinaId,
                        Nome = model.Nome,
                        HoraInicio = TimeSpan.Parse(model.HoraInicio),
                        HoraFim = TimeSpan.Parse(model.HoraFim)
                    };

                    _context.Turnos.Add(novoTurno);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Erro ao salvar no banco de dados: " + ex.Message });
                }
            }

            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

            return Json(new { success = false, errors = errors });
        }

        /// <summary>
        /// Remove um Turno e suas paradas via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoverTurnoOEE(int turnoId)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                if (turnoId <= 0) return Json(new { success = false, message = "ID do turno inválido." });

                var turnoParaRemover = await _context.Turnos
                    .Include(t => t.ParadasPlanejadas)
                    .FirstOrDefaultAsync(t => t.Id == turnoId && t.MaquinaId == maquinaId);

                if (turnoParaRemover == null)
                {
                    return Json(new { success = false, message = "Turno não encontrado ou não pertence a esta máquina." });
                }

                _context.Turnos.Remove(turnoParaRemover);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao remover o turno." });
            }
        }

        /// <summary>
        /// Adiciona uma Parada Planejada via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AdicionarParadaOEE([FromForm(Name = "InputParada")] InputParadaViewModel model)
        {
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
            if (string.IsNullOrEmpty(maquinaId))
            {
                return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
            }

            if (model.TurnoId <= 0)
            {
                ModelState.AddModelError("InputParada.Descricao", "ID do Turno inválido. Recarregue a página.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var turnoPai = await _context.Turnos
                        .Include(t => t.ParadasPlanejadas)
                        .FirstOrDefaultAsync(t => t.Id == model.TurnoId && t.MaquinaId == maquinaId);

                    if (turnoPai == null)
                    {
                        return Json(new { success = false, message = "Turno não encontrado ou inválido." });
                    }

                    // Validação de Duração
                    double minutosTurno;
                    if (turnoPai.HoraFim >= turnoPai.HoraInicio)
                        minutosTurno = (turnoPai.HoraFim - turnoPai.HoraInicio).TotalMinutes;
                    else
                        minutosTurno = (TimeSpan.FromHours(24) - turnoPai.HoraInicio + turnoPai.HoraFim).TotalMinutes;

                    double minutosOcupados = turnoPai.ParadasPlanejadas.Sum(p => p.DuracaoMinutos);

                    if ((minutosOcupados + model.DuracaoMinutos) > minutosTurno)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Não é possível adicionar. O turno tem {minutosTurno} min e já possui {minutosOcupados} min de paradas. Adicionar {model.DuracaoMinutos} min excederia o total."
                        });
                    }

                    var novaParada = new ParadaPlanejada
                    {
                        Descricao = model.Descricao,
                        DuracaoMinutos = model.DuracaoMinutos,
                        TurnoId = model.TurnoId
                    };

                    _context.ParadasPlanejadas.Add(novaParada);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Erro ao salvar no banco de dados: " + ex.Message });
                }
            }

            var errors = ModelState.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

            return Json(new { success = false, errors = errors });
        }

        /// <summary>
        /// Remove uma Parada Planejada via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoverParadaOEE(int paradaId)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                if (paradaId <= 0) return Json(new { success = false, message = "ID da parada inválido." });

                var paradaParaRemover = await _context.ParadasPlanejadas
                    .Include(p => p.Turno)
                    .FirstOrDefaultAsync(p => p.Id == paradaId && p.Turno.MaquinaId == maquinaId);

                if (paradaParaRemover == null)
                {
                    return Json(new { success = false, message = "Parada não encontrada ou não pertence a esta máquina." });
                }

                _context.ParadasPlanejadas.Remove(paradaParaRemover);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao remover a parada." });
            }
        }

        #endregion

        #region GESTÃO DE USUÁRIOS (POST/AJAX)

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

            var adminLogadoId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == adminLogadoId && !editarIsAdmin)
            {
                return Json(new { success = false, message = "Não pode remover a sua própria permissão de Administrador." });
            }

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

        #endregion

        #region REGRAS DE NOTIFICAÇÃO (AJAX)

        [HttpGet]
        public async Task<IActionResult> ObterRegrasUsuario(string userId)
        {
            var regras = await _context.RegrasNotificacao
                                        .Where(r => r.UserId == userId)
                                        .Select(r => r.TipoEvento)
                                        .ToListAsync();

            var statusRegras = new Dictionary<string, bool>
            {
                { "Alarme", regras.Contains("Alarme") },
                { "Aviso", regras.Contains("Aviso") },
                { "Status", regras.Contains("Status") }
            };

            return Json(new { success = true, regras = statusRegras });
        }

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
                _context.RegrasNotificacao.Add(new RegraNotificacao { UserId = userId, TipoEvento = tipoEvento });
                await _context.SaveChangesAsync();
            }
            else if (!ativo && regraExistente != null)
            {
                _context.RegrasNotificacao.Remove(regraExistente);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        #endregion

        #region LANÇAMENTO MANUAL DE REFUGO (POST/AJAX)

        /// <summary>
        /// Registra o Lançamento Manual de Refugo via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> LancarRefugoManual([FromBody] RefugoManualApiModel model)
        {
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");

            if (string.IsNullOrEmpty(maquinaId) || maquinaId != model.MaquinaId)
            {
                return Json(new { success = false, message = "Sessão expirada ou máquina inválida." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dados inválidos: " + string.Join("; ", errors) });
            }

            try
            {
                var novoRefugo = new EventoMaquina
                {
                    TipoEvento = "RefugoManual",
                    CodigoEvento = "REFUGO_MANUAL",
                    Origem = maquinaId,
                    Valor = model.Quantidade.ToString(),
                    Informacao = $"Refugo manual lançado: {model.Quantidade} peças.",
                    Timestamp = model.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow
                };

                _context.EventosMaquina.Add(novoRefugo);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Refugo registrado com sucesso." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao registrar o refugo." });
            }
        }

        #endregion

        #region MÉTODOS AUXILIARES

        // Função auxiliar para popular a lista de usuários e suas permissões.
        private async Task<List<UsuarioInfoViewModel>> PopulaListaUsuarios(GestaoUsuariosViewModel viewModel = null)
        {
            var lista = new List<UsuarioInfoViewModel>();
            var usuariosDoBanco = await _userManager.Users.ToListAsync();

            foreach (var user in usuariosDoBanco)
            {
                lista.Add(new UsuarioInfoViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
                    TelegramChatId = user.TelegramChatId
                });
            }

            if (viewModel != null)
            {
                viewModel.Usuarios = lista;
            }

            return lista;
        }

        #endregion
    }
}