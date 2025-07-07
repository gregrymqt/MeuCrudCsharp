using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages
{
    public class IndexModel : PageModel
    {
        // Voc� pode criar propriedades para passar dados para a p�gina
        public string? Mensagem { get; private set; }

        // O m�todo OnGet() � chamado quando a p�gina � acessada via HTTP GET.
        // � aqui que voc� coloca a l�gica para preparar a p�gina.
        public void OnGet()
        {
            Mensagem = "Bem-vindo ao meu CRUD feito com Razor Pages!";
        }
    }
}
