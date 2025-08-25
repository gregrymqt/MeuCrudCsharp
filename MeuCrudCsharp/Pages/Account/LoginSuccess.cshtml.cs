using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Account
{
    [AllowAnonymous] // Essencial para que a página possa ser acessada antes do login ser "visto"
    public class LoginSuccessModel : PageModel
    {
        // Usamos HttpUtility.UrlDecode para garantir que a URL esteja no formato correto
        private string? _returnUrl;

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl
        {
            get => _returnUrl;
            set => _returnUrl = string.IsNullOrEmpty(value) ? "/" : HttpUtility.UrlDecode(value);
        }

        public void OnGet()
        {
            // O PageModel apenas passa a ReturnUrl para a View.
            // Toda a lógica de delay e redirecionamento é feita no JavaScript para a melhor UX.
        }
    }
}
