using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.auth
{
    [AllowAnonymous]
    public class googleLoginModel : PageModel
    {
        // O construtor e a inje��o do AccountController foram removidos.
        // A PageModel n�o deve chamar m�todos de um Controller diretamente.

        public IActionResult OnGet()
        {
            // Se o usu�rio j� estiver autenticado, redireciona para a p�gina inicial.
            if (User.Identity.IsAuthenticated)
            {
                // Use RedirectToAction se "Home" for um Controller,
                // ou RedirectToPage se "/Index" for uma Razor Page.
                return RedirectToAction("Index", "Home");
            }

            // Se n�o estiver logado, simplesmente renderiza a p�gina para que
            // o usu�rio possa clicar no bot�o de login.
            return Page();
        }

        // O m�todo OnPost() foi removido, pois n�o usaremos um formul�rio.
        // O bot�o de login ser� um link direto para a action do controller.
    }
}
