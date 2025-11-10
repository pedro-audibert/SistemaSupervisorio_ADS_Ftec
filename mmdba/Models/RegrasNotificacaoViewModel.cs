// Models/RegrasNotificacaoViewModel.cs

using System.Collections.Generic;
using mmdba.Models.Entidades;

namespace mmdba.Models
{
    public class RegrasNotificacaoViewModel
    {
        // Lista de todos os usuários cadastrados
        public List<UsuarioInfoViewModel> UsuariosComPermissao { get; set; }

        // Modelo para a edição de regras (será preenchido via AJAX)
        public Dictionary<string, bool> RegrasPorUsuario { get; set; } // <TipoEvento, Ativo>
    }
}