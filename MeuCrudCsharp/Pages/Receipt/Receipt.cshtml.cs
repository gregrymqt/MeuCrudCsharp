using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.ViewModels;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

[Authorize]
public class ReceiptModel : PageModel
{
    // MUDANÇA 1: Injetando as dependências corretas
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<ReceiptModel> _logger;

    public ReceiptModel(IUserAccountService userAccountService, ILogger<ReceiptModel> logger)
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    public ReceiptViewModel Receipt { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string paymentId)
    {
        // MUDANÇA 2: Bloco try-catch para lidar com todas as possíveis falhas
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                _logger.LogWarning(
                    "Tentativa de acesso ao recibo com um userId inválido: {UserId}",
                    userIdString
                );
                return Unauthorized();
            }

            // MUDANÇA 3: Chamando o serviço para buscar os dados
            var payment = await _userAccountService.GetPaymentForReceiptAsync(
                userIdString,
                paymentId
            );

            // Regra de negócio: apenas recibos de pagamentos aprovados podem ser vistos.
            if (payment.Status != "aprovado")
            {
                _logger.LogWarning(
                    "Tentativa de acesso a recibo de pagamento não aprovado. User: {UserId}, Payment: {PaymentId}, Status: {Status}",
                    userIdString,
                    paymentId,
                    payment.Status
                );
                // Forbid é o resultado correto para acesso não autorizado a um recurso válido.
                return Forbid();
            }

            // Mapeamento para o ViewModel (lógica de apresentação)
            Receipt = new ReceiptViewModel
            {
                CompanyName = "Nome da Sua Empresa",
                CompanyCnpj = "XX.XXX.XXX/0001-XX",
                PaymentId = payment.Id.ToString(),
                PaymentDate = payment.DateApproved ?? DateTime.Now,
                CustomerName = payment.User.Name,
                CustomerCpf = payment.CustomerCpf,
                Amount = payment.Amount,
                PaymentMethod = $"Cartão de Crédito final {payment.LastFourDigits}",
            };

            return Page();
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso de pagamento não encontrado.");
            // Se o serviço não encontrou o pagamento, retorna 404 Not Found.
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro inesperado ao gerar o recibo para o pagamento {PaymentId}",
                paymentId
            );
            TempData["ErrorMessage"] =
                "Não foi possível gerar seu recibo no momento. Tente novamente mais tarde.";
            return RedirectToPage("/Profile/Index"); // Redireciona para uma página segura
        }
    }
}
