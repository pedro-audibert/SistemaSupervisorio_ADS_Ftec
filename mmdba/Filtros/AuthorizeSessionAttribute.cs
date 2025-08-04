/*
============================================================================================
FILTRO DE AÇÃO: AuthorizeSessionAttribute
============================================================================================
Finalidade: Este filtro verifica se um usuário está logado (verificando a sessão)
            ANTES de executar o método do controller ao qual ele for aplicado.
            Se o usuário não estiver logado, ele o redireciona para a página de login.
============================================================================================
*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters; // Namespace necessário para filtros
using Microsoft.AspNetCore.Http; // Namespace para usar HttpContext.Session

namespace mmdba.Filters // Use o namespace da pasta que você criou
{
    // Nosso atributo herda de ActionFilterAttribute, que nos dá acesso ao ciclo de vida de uma action.
    public class AuthorizeSessionAttribute : ActionFilterAttribute
    {
        // Este método é executado ANTES do método do controller (ex: antes do 'Index').
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Tenta obter o valor da sessão "UsuarioLogado".
            var usuarioLogado = context.HttpContext.Session.GetString("UsuarioLogado");

            // Verifica se a string está vazia ou nula.
            if (string.IsNullOrEmpty(usuarioLogado))
            {
                // Se não há usuário logado, nós INTERROMPEMOS a execução normal.
                // Em vez de ir para a action original, definimos um novo resultado:
                // um redirecionamento para a Action "Login" do "LoginController".
                context.Result = new RedirectToActionResult("Login", "Login", null);
            }

            // Se o usuário ESTIVER logado, não fazemos nada e a execução continua normalmente.
            base.OnActionExecuting(context);
        }
    }
}