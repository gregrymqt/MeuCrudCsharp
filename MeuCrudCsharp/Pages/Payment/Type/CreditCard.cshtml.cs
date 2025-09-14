using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Payments.Controllers;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Pages.Payment.Type
{
    [Authorize]
    public class CreditCardModel : PageModel
    {
        private readonly PreferenceController _preferenceController;
        private readonly ILogger<CreditCardModel> _logger; // MUDANÇA 1: Adicionando Logger
        private readonly MercadoPagoSettings _mercadoPagoSettings;

        // Apenas o 'Plano' vem da URL. O 'Valor' será definido no backend.
        [BindProperty(SupportsGet = true)]
        public string? Plano { get; set; }

        public decimal Valor { get; private set; } // Não é mais um BindProperty
        public string? PreferenceId { get; private set; }
        public string? PreapprovalPlanId { get; private set; }
        public Users? user { get; set; }


        public CreditCardModel(
            PreferenceController preferenceController,
            IConfiguration configuration,
            ILogger<CreditCardModel> logger,
            IOptions<MercadoPagoSettings> mercadoPagoSettings
            ) // MUDANÇA 1
        {
            _preferenceController = preferenceController;
            _logger = logger;
            _mercadoPagoSettings = mercadoPagoSettings.Value;
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

            if (User.Identity is not ClaimsIdentity identity)
            {
                _logger.LogWarning("Usuário não autenticado tentou acessar a página de pagamento.");
                return RedirectToPage("/Account/Login"); // Redireciona para a página de login
            }
            else
            {
                user = new Users
                {
                    UserName = identity.Name,
                    Email = identity.FindFirst(ClaimTypes.Email)?.Value,
                    Id = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
            }

                // MUDANÇA 3: Lógica para buscar o valor e o ID da configuração
                try
                {
                    var planoNormalizado = Plano.Capitalize(); // Um método de extensão para "Mensal" ou "Anual"

                    if (_mercadoPagoSettings.Plans.TryGetValue(planoNormalizado, out PlanDetail planoSelecionado))
                    {
                        Valor = planoSelecionado.Price;
                        PreapprovalPlanId = planoSelecionado.Id;
                    }

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

                    // Cria a preferência de pagamento com o VALOR SEGURO
                    var preferenceID = await _preferenceController.Create(Valor);
                    PreferenceId = preferenceID.ToString();

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
