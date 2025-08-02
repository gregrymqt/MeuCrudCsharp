using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.UserAccount.Controllers // Namespace corrigido
{
    [ApiController]
    [Route("api/subscriptions")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ApiDbContext _context; // NOVO: Injetando o DbContext

        public SubscriptionsController(IMercadoPagoService mercadoPagoService, ApiDbContext context)
        {
            _mercadoPagoService = mercadoPagoService;
            _context = context; // NOVO
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubscription(
            [FromBody] CreateSubscriptionDto createDto
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var subscriptionResponse = await _mercadoPagoService.CreateSubscriptionAsync(
                    createDto
                );

                // --- MUDANÇA PRINCIPAL: SALVANDO A ASSINATURA NO BANCO ---

                // 1. Pegar o ID do usuário logado
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    return Unauthorized("ID de usuário inválido.");
                }

                // 2. Encontrar o nosso 'Plan' local a partir do ID do plano do Mercado Pago
                var localPlan = await _context.Plans.FirstOrDefaultAsync(p =>
                    p.ExternalPlanId == subscriptionResponse.PreapprovalPlanId
                );
                if (localPlan == null)
                {
                    // Lançar erro se o plano não existir no nosso banco
                    throw new InvalidOperationException(
                        "Plano correspondente não encontrado no banco de dados."
                    );
                }

                // 3. Criar a nova entidade de Assinatura
                var newSubscription = new Subscription
                {
                    UserId = userId,
                    PlanId = localPlan.Id, // Chave estrangeira para o nosso 'Plan'
                    ExternalId = subscriptionResponse.Id,
                    Status = subscriptionResponse.Status,
                    PayerEmail = subscriptionResponse.PayerEmail,
                    CreatedAt = DateTime.UtcNow,
                };

                // 4. Adicionar ao context e salvar
                _context.Subscriptions.Add(newSubscription);
                await _context.SaveChangesAsync();

                // --- FIM DA MUDANÇA ---

                return Ok(subscriptionResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        message = "Ocorreu um erro ao processar sua assinatura.",
                        error = ex.Message,
                    }
                );
            }
        }
    }
}
