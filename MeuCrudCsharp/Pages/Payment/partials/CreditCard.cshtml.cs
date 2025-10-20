using System.Globalization;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Payments.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Pages.Payment.partials;

// O ViewModel não precisa de alterações. Ele já suporta ambos os IDs.
public class CreditCardViewModel
{
    public string? PlanName { get; set; }
    public decimal Price { get; set; }
    public string FormattedPrice => Price.ToString("C", new CultureInfo("pt-BR"));
    public string? PreferenceId { get; set; }
    public string? PreapprovalPlanId { get; set; }
    public string? PublicKey { get; set; }
}

public class CreditCardModel : PageModel
{
    private readonly PreferenceController _preferenceController;
    private readonly ILogger<CreditCardModel> _logger;
    private readonly MercadoPagoSettings _mercadoPagoSettings;
    private readonly ApiDbContext _dbContext;

    [BindProperty]
    public CreditCardViewModel ViewModel { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Plano { get; set; }

    public CreditCardModel(
        PreferenceController preferenceController,
        ILogger<CreditCardModel> logger,
        IOptions<MercadoPagoSettings> mercadoPagoSettings,
        ApiDbContext dbContext
    )
    {
        _preferenceController = preferenceController;
        _logger = logger;
        _mercadoPagoSettings = mercadoPagoSettings.Value;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Plano))
        {
            _logger.LogWarning(
                "Tentativa de acesso à página de pagamento sem um plano especificado."
            );
            return RedirectToPage("/Subscription/Index");
        }

        try
        {
            var planoNormalizado = Plano.Capitalize();
            var planoSelecionado = _dbContext.Plans.FirstOrDefault(p => p.Name == planoNormalizado);

            if (planoSelecionado == null)
            {
                _logger.LogWarning("Plano inválido ou não configurado solicitado: {Plano}", Plano);
                TempData["ErrorMessage"] = $"O plano '{Plano}' não foi encontrado.";
                return RedirectToPage("/Subscription/Index");
            }

            // Popula as informações básicas do ViewModel
            ViewModel.PlanName = planoNormalizado;
            ViewModel.Price = planoSelecionado.TransactionAmount;
            ViewModel.PublicKey = _mercadoPagoSettings.PublicKey;

            ViewModel.PreapprovalPlanId = planoSelecionado.ExternalPlanId;

            var preference = await _preferenceController.Create(ViewModel.Price);
            ViewModel.PreferenceId = preference.ToString();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro crítico ao preparar a página de pagamento para o plano {Plano}",
                Plano
            );
            TempData["ErrorMessage"] =
                "Ocorreu um erro inesperado ao preparar seu pagamento. Por favor, tente novamente.";
            return RedirectToPage("/Subscription/Index");
        }
    }
}

// A extensão 'Capitalize' permanece a mesma
public static class StringExtensions
{
    public static string Capitalize(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        return char.ToUpper(input[0]) + input.Substring(1).ToLower();
    }
}
