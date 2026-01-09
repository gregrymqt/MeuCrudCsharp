using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Controllers
{
    [Route("api/[controller]")] // Rota final: api/refunds
    public class RefundsController : MercadoPagoApiControllerBase
    {
        private readonly IRefundService _refundService;

        public RefundsController(IRefundService refundService)
        {
            _refundService = refundService;
        }

        [HttpPost("{paymentId}")]
        public async Task<IActionResult> RequestRefund([FromRoute] string paymentId)
        {
            try
            {
                // Validação básica: O ID do pagamento no MP é numérico (Long),
                // mas chega como string na rota. Validamos isso antes de chamar a service.
                if (!long.TryParse(paymentId, out var paymentIdLong))
                {
                    return BadRequest(
                        new { success = false, message = "O ID do pagamento deve ser numérico." }
                    );
                }

                // Chama o serviço passando o ID convertido
                await _refundService.RequestRefundAsync(paymentIdLong);

                return Ok(new { success = true, message = "Reembolso solicitado com sucesso." });
            }
            catch (Exception ex)
            {
                // Usa seu método da classe base para padronizar o erro (400, 404 ou 500)
                return HandleException(ex, "Não foi possível processar o reembolso.");
            }
        }
    }
}
