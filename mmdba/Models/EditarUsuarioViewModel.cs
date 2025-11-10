using System.ComponentModel.DataAnnotations;

namespace mmdba.Models
{
    public class EditarUsuarioViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "É Administrador")]
        public bool IsAdmin { get; set; }
    }
}