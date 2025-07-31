using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.Admin.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    [Authorize] // Garante que apenas usuários logados possam criar assinaturas
    public class SubscriptionsController : ControllerBase
    {
        private readonly IMercadoPagoService _mercadoPagoService;

        public SubscriptionsController(IMercadoPagoService mercadoPagoService)
        {
            _mercadoPagoService = mercadoPagoService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Opcional: Adicionar o e-mail do usuário logado para segurança
                // createDto.PayerEmail = User.Identity.Name;

                var subscriptionResponse = await _mercadoPagoService.CreateSubscriptionAsync(createDto);

                // Aqui você deve salvar o ID da assinatura (subscriptionResponse.Id)
                // no seu banco de dados, associado ao seu usuário.

                return Ok(subscriptionResponse);
            }
            catch (Exception ex)
            {
                // Retorna um erro claro se a comunicação com o Mercado Pago falhar
                return StatusCode(500, new { message = "Ocorreu um erro ao processar sua assinatura.", error = ex.Message });
            }
        }
    }
}
