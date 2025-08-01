using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs; // Precisaremos do DTO de request
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.UserAccount.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user/subscription")] // Rota base para a API de assinatura do usuário
    public class SubscriptionApiController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public SubscriptionApiController(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        [HttpPut("card")] // Rota: PUT api/user/subscription/card
        public async Task<IActionResult> ChangeCard([FromBody] UpdateCardTokenDto request)
        {
            var userId = GetCurrentUserId();
            var success = await _userAccountService.UpdateSubscriptionCardAsync(userId, request.NewCardToken);

            if (!success)
            {
                return NotFound(new { message = "Assinatura não encontrada ou falha na atualização." });
            }

            return Ok(new { message = "Cartão da assinatura atualizado com sucesso." });
        }

        [HttpPost("cancel")] // Rota: POST api/user/subscription/cancel
        public async Task<IActionResult> CancelSubscription()
        {
            var userId = GetCurrentUserId();
            var success = await _userAccountService.CancelSubscriptionAsync(userId);

            if (!success)
            {
                return NotFound(new { message = "Assinatura não encontrada ou já está cancelada." });
            }

            return Ok(new { message = "Assinatura cancelada com sucesso." });
        }

        [HttpPost("reactivate")] // Rota: POST api/user/subscription/reactivate
        public async Task<IActionResult> ReactivateSubscription()
        {
            var userId = GetCurrentUserId();
            var success = await _userAccountService.ReactivateSubscriptionAsync(userId);

            if (!success)
            {
                return NotFound(new { message = "Assinatura não encontrada ou não está em um estado que permita reativação." });
            }

            return Ok(new { message = "Assinatura reativada com sucesso." });
        }

        // Método auxiliar para pegar o ID do usuário logado de forma segura
        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Não foi possível obter a identificação do usuário.");
        }
    }
}