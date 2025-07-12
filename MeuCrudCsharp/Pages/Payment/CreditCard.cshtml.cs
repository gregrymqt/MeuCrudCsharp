using System.Security.Claims;
using System.Threading.Tasks; // Necess�rio para Task
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization; // Para proteger a p�gina
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Payment
{
    [Authorize] // Garante que apenas usu�rios logados podem acessar esta p�gina
    public class CreditCardModel : PageModel
    {
        // Inje��es de depend�ncia corretas
        private readonly IPreferencePayment _preferencePayment;
        private readonly IConfiguration _configuration;

        public CreditCardModel(IPreferencePayment preferencePayment, IConfiguration configuration)
        {
            _preferencePayment = preferencePayment;
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            try
            {
                var preference = await _preferencePayment.CreatePreferenceAsync(
                    (decimal)100.00,
                    this.User
                );

                var publicKeyFromConfig = _configuration["MercadoPago:PublicKey"];

                ViewData["PreferenceId"] = preference.Id;
                ViewData["PublicKey"] = publicKeyFromConfig;
            }
            catch (System.Exception ex)
            {
                // � uma boa pr�tica tratar poss�veis erros, como falha na comunica��o com a API.
                // Aqui voc� poderia logar o erro e talvez redirecionar para uma p�gina de erro.
                // Por enquanto, vamos apenas definir os valores como nulos.
                ViewData["PreferenceId"] = null;
                ViewData["PublicKey"] = null;
                // Log o erro: _logger.LogError(ex, "Falha ao criar prefer�ncia de pagamento.");
            }
        }
    }
}
