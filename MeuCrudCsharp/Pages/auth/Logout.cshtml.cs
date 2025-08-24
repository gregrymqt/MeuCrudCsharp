using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Auth
{
    public class LogoutModel : PageModel
    {
        // OnGet é usado apenas para exibir a página com o botão de confirmação.
        public void OnGet() { }

        // OnPostAsync é chamado quando o formulário é enviado.
        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Desfaz a autenticação no lado do servidor (limpa o HttpContext.User)
            // Isso invalida o esquema de autenticação para a sessão atual.
            await HttpContext.SignOutAsync();

            // 2. Remove o cookie JWT do navegador do usuário
            // Isso garante que ele não possa ser usado em requisições futuras.
            Response.Cookies.Delete("jwt");

            // 3. Redireciona o usuário para a página inicial (ou outra página pública)
            return RedirectToPage("/Index");
        }
    }
}
