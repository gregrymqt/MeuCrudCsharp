using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Pages.auth
{
    public class googleLoginModel : PageModel
    {
        // O construtor e a injeção do AccountController foram removidos.
        // A PageModel não deve chamar métodos de um Controller diretamente.

        public IActionResult OnGet()
        {
            // Se o usuário já estiver autenticado, redireciona para a página inicial.
            if (User.Identity.IsAuthenticated)
            {
                // Use RedirectToAction se "Home" for um Controller, 
                // ou RedirectToPage se "/Index" for uma Razor Page.
                return RedirectToAction("Index", "Home");
            }

            // Se não estiver logado, simplesmente renderiza a página para que 
            // o usuário possa clicar no botão de login.
            return Page();
        }

        // O método OnPost() foi removido, pois não usaremos um formulário.
        // O botão de login será um link direto para a action do controller.
    }
}