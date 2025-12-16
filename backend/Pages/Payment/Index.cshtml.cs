using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Payment
{
    public class IndexModel : PageModel
    {
        // A anotação [BindProperty] com SupportsGet = true faz com que o Razor Pages
        // automaticamente pegue o valor do parâmetro "plano" da URL (query string)
        // e o atribua a esta propriedade.
        [BindProperty(SupportsGet = true)]
        public string? Plano { get; set; }

        // Propriedade para validação e para deixar o código mais limpo na View.
        public bool IsPlanoMensal { get; set; }

        public void OnGet()
        {
            // Verificamos se o plano é "mensal" de forma segura (ignorando maiúsculas/minúsculas)
            IsPlanoMensal =
                !string.IsNullOrEmpty(Plano)
                && Plano.Equals("mensal", StringComparison.OrdinalIgnoreCase);

            // Se nenhum plano for passado ou for inválido, podemos definir um padrão
            // ou redirecionar. Por enquanto, vamos assumir que o plano anual é o padrão
            // se não for mensal.
            if (string.IsNullOrEmpty(Plano))
            {
                Plano = "anual"; // Define um padrão
            }
        }
    }
}
