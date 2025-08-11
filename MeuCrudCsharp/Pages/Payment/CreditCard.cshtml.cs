using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Pages.Payment
{
    [Authorize]
    public class CreditCardModel : PageModel
    {
        private readonly IPreferencePayment _preferencePayment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CreditCardModel> _logger; // MUDANÇA 1: Adicionando Logger

        // Apenas o 'Plano' vem da URL. O 'Valor' será definido no backend.
        [BindProperty(SupportsGet = true)]
        public string? Plano { get; set; }

        public decimal Valor { get; private set; } // Não é mais um BindProperty
        public string? PublicKey { get; private set; }
        public string? PreferenceId { get; private set; }
        public string? PreapprovalPlanId { get; private set; }

        public CreditCardModel(
            IPreferencePayment preferencePayment,
            IConfiguration configuration,
            ILogger<CreditCardModel> logger
        ) // MUDANÇA 1
        {
            _preferencePayment = preferencePayment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // MUDANÇA 2: Validação robusta do plano
            if (
                string.IsNullOrEmpty(Plano)
                || (Plano.ToLower() != "mensal" && Plano.ToLower() != "anual")
            )
            {
                _logger.LogWarning(
                    "Tentativa de acesso à página de pagamento com plano inválido: {Plano}",
                    Plano
                );
                return RedirectToPage("/Subscription/Index"); // Volta para a página de planos
            }

            // MUDANÇA 3: Lógica para buscar o valor e o ID da configuração
            try
            {
                // Usamos a capitalização correta para buscar no appsettings (Mensal, Anual)
                var planConfigKey = $"MercadoPago:Plans:{Plano.Capitalize()}";

                Valor = _configuration.GetValue<decimal>($"{planConfigKey}:Price");
                PreapprovalPlanId = _configuration.GetValue<string>($"{planConfigKey}:Id");

                // Validação para garantir que a configuração foi encontrada
                if (Valor <= 0)
                {
                    throw new InvalidOperationException(
                        $"O preço para o plano '{Plano}' não está configurado ou é inválido."
                    );
                }
                if (Plano.ToLower() == "anual" && string.IsNullOrEmpty(PreapprovalPlanId))
                {
                    throw new InvalidOperationException(
                        $"O PreapprovalPlanId para o plano '{Plano}' não está configurado."
                    );
                }

                // Carrega a chave pública para o frontend
                PublicKey = _configuration["MercadoPago:PUBLIC_KEY"];

                // Cria a preferência de pagamento com o VALOR SEGURO
                var preference = await _preferencePayment.CreatePreferenceAsync(Valor, User);
                PreferenceId = preference.Id;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao configurar a página de pagamento para o plano {Plano}",
                    Plano
                );
                // Adicione uma mensagem de erro para o usuário, se desejar
                TempData["ErrorMessage"] =
                    "Não foi possível preparar seu pagamento. Tente novamente mais tarde.";
                return RedirectToPage("/Subscription/Index");
            }
        }
    }

    // Pequena extensão para facilitar a capitalização (pode ir em um arquivo separado)
    public static class StringExtensions
    {
        public static string Capitalize(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }
}
