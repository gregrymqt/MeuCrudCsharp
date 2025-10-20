// Pix.cshtml.cs
using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Pages.Payment.partials; // Supondo que Users esteja aqui
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options; // Para ler as configurações

// Crie esta classe para organizar os dados para a View
public class PixViewModel
{
    public string PublicKey { get; set; }
    public decimal TransactionAmount { get; set; }
    public string Description { get; set; }
}

[Authorize]
public class PixModel : PageModel // Renomeado de 'Pix' para 'PixModel' por convenção
{
    // Supondo que você tenha uma classe de configurações como a do Cartão
    private readonly MercadoPagoSettings _mercadoPagoSettings;
    private readonly ILogger<PixModel> _logger;
    private readonly ApiDbContext _dbContext;

    [BindProperty]
    public PixViewModel ViewModel { get; set; } = new();

    // Recebe o plano da URL, assim como no Cartão de Crédito
    [BindProperty(SupportsGet = true)]
    public string? Plano { get; set; }

    public PixModel(
        ILogger<PixModel> logger,
        IOptions<MercadoPagoSettings> mercadoPagoSettings,
        ApiDbContext dbContext
    )
    {
        _logger = logger;
        _mercadoPagoSettings = mercadoPagoSettings.Value;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGet()
    {
        try
        {
            var planoNormalizado = Plano.Capitalize();
            var planoSelecionado = _dbContext.Plans.FirstOrDefault(p => p.Name == planoNormalizado);

            // Lógica para pegar os detalhes do plano (similar ao Cartão de Crédito)
            if (planoSelecionado == null)
            {
                TempData["ErrorMessage"] = $"Plano '{Plano}' não encontrado.";
                return RedirectToPage("/Subscription/Index");
            }

            // Popula o ViewModel com TODOS os dados necessários
            ViewModel = new PixViewModel
            {
                PublicKey = _mercadoPagoSettings.PublicKey,
                TransactionAmount = planoSelecionado.TransactionAmount,
                Description = $"Assinatura Plano {Plano.Capitalize()}",
            };

            return Page();
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Erro ao configurar a página de pagamento PIX para o plano {Plano}",
                Plano
            );
            TempData["ErrorMessage"] = "Ocorreu um erro ao preparar o pagamento.";
            return RedirectToPage("/Subscription/Index");
        }
    }
}
