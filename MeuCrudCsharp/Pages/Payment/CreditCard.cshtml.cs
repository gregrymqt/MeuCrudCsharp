using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization; // Para proteger a p�gina
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration; // Necessário para IConfiguration

namespace MeuCrudCsharp.Pages.Payment
{
    [Authorize] // Garante que apenas usu�rios logados podem acessar esta p�gina
    public class CreditCardModel : PageModel
    {
        // Inje��es de depend�ncia corretas
        private readonly IPreferencePayment _preferencePayment;
        private readonly IConfiguration _configuration;

        [BindProperty(SupportsGet = true)]
        public string? Plano { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal Valor { get; set; }

        // --- PROPRIEDADES PARA ENVIAR DADOS PARA O JAVASCRIPT ---
        public string? PublicKey { get; private set; }
        public string? PreferenceId { get; private set; } // Se você usar preferenceId

        public string? PreapprovalPlanId { get; private set; }

        public CreditCardModel(IPreferencePayment preferencePayment, IConfiguration configuration)
        {
            _preferencePayment = preferencePayment;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Plano))
            {
                return RedirectToPage("/Subscription/Index"); // Volta se não houver plano
            }

            var preference = await _preferencePayment.CreatePreferenceAsync(Valor, this.User);
            // Validação simples para garantir que o valor é válido
            if (Valor <= 0)
            {
                // Redireciona de volta para a página de planos se os dados estiverem incorretos
                return RedirectToPage("/Subscription/Index");
            }

            // Carrega as configurações do seu appsettings.json para enviar ao frontend
            PublicKey = _configuration["MercadoPago:PublicKey"];
            PreferenceId = preference.Id;

            PreapprovalPlanId = _configuration[$"MercadoPago:Plans:{Plano}"];

            if (string.IsNullOrEmpty(PreapprovalPlanId))
            {
                // Lida com o caso de um plano inválido na URL
                // Logar erro, redirecionar, etc.
                return BadRequest("Plano de assinatura inválido.");
            }

            return Page();
        }
    }
}
