using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Payments;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : MercadoPagoApiControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IUserContext _userContext;

    public PaymentsController(IPaymentService paymentService, IUserContext userContext)
    {
        _paymentService = paymentService;
        _userContext = userContext;
    }

    [HttpGet("history")] // Rota final: GET api/payments/history
    [Authorize]
    public async Task<IActionResult> GetPaymentHistory()
    {
        try
        {
            // 1. Pega o ID do usuário logado via Token
            var userId = _userContext.GetCurrentUserId().ToString();

            // 2. Chama a service
            var history = await _paymentService.GetUserPaymentHistoryAsync(userId);

            return Ok(history);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Erro ao recuperar histórico de pagamentos.");
        }
    }
}
