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
using Microsoft.AspNetCore.Http; // Para aceder ao HttpContext.Session
using mmdba.Models; // Para acedermos ao novo ParametrosOEEViewModel
using Microsoft.EntityFrameworkCore; // Para usarmos o .Include() e .FirstOrDefaultAsync()
using System; // Já deve existir, mas garanta
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

        // Action para a página de Parâmetros de OEE.
        [HttpGet]
        public async Task<IActionResult> ParametrosOEE()
        {
            ViewData["ActivePage"] = "ParametrosOEE";

            // 1. Obter a máquina selecionada da Sessão (definida no HomeController)
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");

            // 2. Se o utilizador aceder a esta página diretamente sem selecionar uma máquina,
            //    redireciona-o para a seleção de máquina.
            if (string.IsNullOrEmpty(maquinaId))
            {
                // Redireciona para /Home/Index
                return RedirectToAction("Index", "Home");
            }

            // 3. Criar o ViewModel que será enviado para a View
            var viewModel = new ParametrosOEEViewModel();

            // 4. Carregar a Velocidade Ideal E Configuração de Qualidade
            var parametros = await _context.OeeParametrosMaquina
                                            .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

            if (parametros != null)
            {
                viewModel.VelocidadeIdealPorHora = parametros.VelocidadeIdealPorHora;
                // --- ADICIONE ESTA LINHA ---
                viewModel.HabilitarRefugoManual = parametros.HabilitarRefugoManual;
            }
            // OBS: Se 'parametros' for null, a 'viewModel' usará o default (0 para int, false para bool), o que está correto.

            // 5. Carregar os Turnos e Paradas (do Passo 1 da nossa estratégia)
            // Usamos .Include() para trazer as ParadasPlanejadas "filhas" de cada Turno.
            viewModel.Turnos = await _context.Turnos
                                        .Where(t => t.MaquinaId == maquinaId)
                                        .Include(t => t.ParadasPlanejadas)
                                        .OrderBy(t => t.HoraInicio)
                                        .ToListAsync();

            // 6. Envia o ViewModel (com os dados preenchidos) para a View
            return View(viewModel);
        }

        /// <summary>
        /// Action (POST) que salva a Velocidade Ideal (Passo 2).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 1. Alterado o tipo de retorno para JsonResult (para funcionar com AJAX)
        public async Task<JsonResult> SalvarVelocidadeOEE(ParametrosOEEViewModel model)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    // 2. Retorna um erro JSON se a sessão expirar
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                // 3. Validação do lado do servidor (o JS já limpou o ".")
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

                // 4. Retorna sucesso em JSON
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // 5. Retorna um erro JSON em caso de falha no banco
                // (Nota: Não estamos a usar ILogger aqui porque não foi injetado no seu SistemaController)
                return Json(new { success = false, message = "Ocorreu um erro interno ao salvar no banco de dados." });
            }
        }


        /// <summary>
        /// Action (POST) que salva um novo Turno (Passo 1) via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AdicionarTurnoOEE(
    [FromForm(Name = "InputTurno")] InputTurnoViewModel model)
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

            // ===== CORRIGIDO: Remove o prefixo "InputTurno." para evitar duplicação =====
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key, // SEM adicionar prefixo - mantém como "Nome", "HoraInicio", etc
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

            return Json(new { success = false, errors = errors });
        }


        /// <summary>
        /// Action (POST) que remove um Turno (e suas Paradas) via AJAX.
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

                // Validação
                if (turnoId <= 0)
                {
                    return Json(new { success = false, message = "ID do turno inválido." });
                }

                // 1. Encontra o Turno
                // IMPORTANTE: Usamos .Include() para carregar as ParadasPlanejadas "filhas"
                // deste turno.
                var turnoParaRemover = await _context.Turnos
                    .Include(t => t.ParadasPlanejadas) // Carrega as paradas filhas
                    .FirstOrDefaultAsync(t => t.Id == turnoId && t.MaquinaId == maquinaId);

                if (turnoParaRemover == null)
                {
                    return Json(new { success = false, message = "Turno não encontrado ou não pertence a esta máquina." });
                }

                // 2. Remove o Turno.
                // Graças ao .Include(), o EF Core saberá que deve remover
                // também as ParadasPlanejadas associadas (em cascata).
                _context.Turnos.Remove(turnoParaRemover);

                // 3. Salva no banco
                await _context.SaveChangesAsync();

                // 4. Retorna sucesso
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao remover o turno." });
            }
        }


        // <summary>
        /// Action (POST) que salva uma nova Parada Planejada (Passo 1) via AJAX.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AdicionarParadaOEE(
            [FromForm(Name = "InputParada")] InputParadaViewModel model)
        {
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
            if (string.IsNullOrEmpty(maquinaId))
            {
                return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
            }

            // Validação de Negócio (TurnoId)
            if (model.TurnoId <= 0)
            {
                ModelState.AddModelError("InputParada.Descricao", "ID do Turno inválido. Recarregue a página.");
            }

            // Validação dos [Required] e [Range] (Descricao, DuracaoMinutos)
            if (ModelState.IsValid)
            {
                try
                {
                    // Verifica se o Turno pertence à máquina da sessão (Segurança)
                    var turnoPai = await _context.Turnos
                        .FirstOrDefaultAsync(t => t.Id == model.TurnoId && t.MaquinaId == maquinaId);

                    if (turnoPai == null)
                    {
                        return Json(new { success = false, message = "Turno não encontrado ou inválido." });
                    }

                    var novaParada = new ParadaPlanejada
                    {
                        Descricao = model.Descricao,
                        DuracaoMinutos = model.DuracaoMinutos,
                        TurnoId = model.TurnoId // Associa ao Turno pai
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

            // Retorna os erros de validação (ex: "InputParada.Descricao")
            var errors = ModelState.ToDictionary(
                kvp => kvp.Key, // Retorna "InputParada.Descricao", etc.
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

            return Json(new { success = false, errors = errors });
        }



        /// <summary>
        /// Action (POST) que remove uma Parada Planejada via AJAX.
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

                // Validação
                if (paradaId <= 0)
                {
                    return Json(new { success = false, message = "ID da parada inválido." });
                }

                // 1. Encontra a Parada
                // Incluímos o Turno para garantir que esta parada
                // pertence à máquina que está na sessão (Segurança).
                var paradaParaRemover = await _context.ParadasPlanejadas
                    .Include(p => p.Turno)
                    .FirstOrDefaultAsync(p => p.Id == paradaId && p.Turno.MaquinaId == maquinaId);

                if (paradaParaRemover == null)
                {
                    return Json(new { success = false, message = "Parada não encontrada ou não pertence a esta máquina." });
                }

                // 2. Remove a Parada
                _context.ParadasPlanejadas.Remove(paradaParaRemover);

                // 3. Salva no banco
                await _context.SaveChangesAsync();

                // 4. Retorna sucesso
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Erro ao remover ParadaPlanejada");
                return Json(new { success = false, message = "Ocorreu um erro interno ao remover a parada." });
            }
        }


        /// <summary>
        /// Action (POST) que salva a configuração de Qualidade (Passo 3) via AJAX (auto-save).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // O parâmetro 'HabilitarRefugoManual' corresponde ao nome do checkbox/toggle na View.
        public async Task<JsonResult> SalvarConfigQualidade(bool HabilitarRefugoManual)
        {
            try
            {
                var maquinaId = HttpContext.Session.GetString("SelectedMachineId");
                if (string.IsNullOrEmpty(maquinaId))
                {
                    return Json(new { success = false, message = "Sessão expirada. Por favor, selecione a máquina novamente." });
                }

                // 1. Encontra os parâmetros existentes (ou cria um novo)
                var parametros = await _context.OeeParametrosMaquina
                                            .FirstOrDefaultAsync(p => p.MaquinaId == maquinaId);

                if (parametros == null)
                {
                    // Se o utilizador mexe nisto antes de salvar a velocidade,
                    // criamos o registo.
                    parametros = new OeeParametrosMaquina
                    {
                        MaquinaId = maquinaId
                    };
                    _context.OeeParametrosMaquina.Add(parametros);
                }

                // 2. Atualiza o valor com o parâmetro recebido
                parametros.HabilitarRefugoManual = HabilitarRefugoManual;

                // 3. Salva no banco
                await _context.SaveChangesAsync();

                // 4. Retorna sucesso
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocorreu um erro interno ao salvar a configuração." });
            }
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

        /// <summary>
        /// Action (POST) que registra o Lançamento Manual de Refugo via AJAX.
        /// Requer a role "Admin" e é protegida pela autenticação de sessão.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> LancarRefugoManual([FromBody] RefugoManualApiModel model)
        {
            var maquinaId = HttpContext.Session.GetString("SelectedMachineId");

            // Valida a sessão e se o MaquinaId no payload corresponde à sessão (Segurança)
            if (string.IsNullOrEmpty(maquinaId) || maquinaId != model.MaquinaId)
            {
                return Json(new { success = false, message = "Sessão expirada ou máquina inválida." });
            }

            if (!ModelState.IsValid)
            {
                // Retorna os erros de validação
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dados inválidos: " + string.Join("; ", errors) });
            }

            try
            {
                // 1. Cria a entidade EventoMaquina
                var novoRefugo = new EventoMaquina
                {
                    TipoEvento = "RefugoManual",
                    CodigoEvento = "REFUGO_MANUAL",
                    Origem = maquinaId,
                    // O valor é a quantidade lançada
                    Valor = model.Quantidade.ToString(),
                    Informacao = $"Refugo manual lançado: {model.Quantidade} peças.",
                    // Usa o timestamp recebido (convertido para UTC) ou UTC Now
                    Timestamp = model.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow
                };

                // 2. Salva no Banco de Dados
                _context.EventosMaquina.Add(novoRefugo);
                await _context.SaveChangesAsync();

                // 3. Sucesso.
                return Json(new { success = true, message = "Refugo registrado com sucesso." });
            }
            catch (Exception ex)
            {
                // Em caso de falha no banco
                return Json(new { success = false, message = "Ocorreu um erro interno ao registrar o refugo." });
            }
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