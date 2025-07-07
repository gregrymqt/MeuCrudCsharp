using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages
{
    public class IndexModel : PageModel
    {
        // Você pode criar propriedades para passar dados para a página
        public string? Mensagem { get; private set; }

        // O método OnGet() é chamado quando a página é acessada via HTTP GET.
        // É aqui que você coloca a lógica para preparar a página.
        public void OnGet()
        {
            Mensagem = "Bem-vindo ao meu CRUD feito com Razor Pages!";
        }
    }
}
