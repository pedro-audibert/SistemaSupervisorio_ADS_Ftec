using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    public class GestaoUsuariosViewModel
    {
        public List<UsuarioInfoViewModel> Usuarios { get; set; }
        public InputNovoUsuarioViewModel InputNovoUsuario { get; set; }

        public GestaoUsuariosViewModel()
        {
            Usuarios = new List<UsuarioInfoViewModel>();
            InputNovoUsuario = new InputNovoUsuarioViewModel();
        }
    }

    public class UsuarioInfoViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }

        // --- PROPRIEDADE ADICIONADA ---
        /// <summary>
        /// Armazena o ChatId do Telegram do usuário (se houver).
        /// Usado pela UI para verificar se o usuário pode receber notificações.
        /// </summary>
        public string? TelegramChatId { get; set; }
        // --- FIM DA ADIÇÃO ---
    }

    public class InputNovoUsuarioViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "O email não é válido")]
        [Display(Name = "Email")]
        public string NovoEmail { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        [StringLength(100, ErrorMessage = "A senha deve ter pelo menos 6 caracteres", MinimumLength = 6)]
        public string NovoPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        [Compare("NovoPassword", ErrorMessage = "As senhas não conferem.")]
        public string NovoConfirmPassword { get; set; }

        [Display(Name = "Tornar este usuário Administrador")]
        public bool NovoIsAdmin { get; set; }
    }
}