using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Controllers
{
    [Route("api/subscriptions")] // Rota dedicada
    public class SubscriptionsController : MercadoPagoApiControllerBase // Herança conforme 
    {
        private readonly IUserSubscriptionService _service;

        public SubscriptionsController(IUserSubscriptionService service)
        {
            _service = service;
        }

        [HttpGet("details")] // GET: api/subscriptions/details
        public async Task<IActionResult> GetDetails()
        {
            try
            {
                var result = await _service.GetMySubscriptionDetailsAsync();

                if (result == null)
                {
                    // Retorna 404 padronizado se não houver assinatura [cite: 4]
                    return NotFound(new { success = false, message = "Nenhuma assinatura encontrada." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Usa o método base para tratar erros 
                return HandleException(ex, "Erro ao buscar detalhes da assinatura.");
            }
        }

        [HttpPut("status")] // PUT: api/subscriptions/status
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateSubscriptionStatusDto request)
        {
            try
            {
                // O front envia { status: "paused" }, mapeamos para a string que o service espera
                await _service.ChangeSubscriptionStatusAsync(request.Status);

                return Ok(new { success = true, message = "Status da assinatura atualizado com sucesso." });
            }
            catch (Exception ex)
            {
                // Trata AppServiceException (Regra de Negócio) e Erros 500 [cite: 2-8]
                return HandleException(ex, "Não foi possível atualizar o status da assinatura.");
            }
        }
    }
}
