// Usings combinados de todos os arquivos

using System.Security.Claims;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.User.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Controllers
{   
    [Route("api/user-account")]
    public class UserAccountController : ApiControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly IClientService _clientService;
        private readonly IRefundService _refundService;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<UserAccountController> _logger;
        private readonly IUserContext _userContext;
        

        public UserAccountController(
            IUserAccountService userAccountService,
            IClientService clientService,
            IRefundService refundService,
            UserManager<Users> userManager,
            ILogger<UserAccountController> logger)
        {
            _userAccountService = userAccountService;
            _clientService = clientService;
            _refundService = refundService;
            _userManager = userManager;
            _logger = logger;
        }
        
        [HttpGet("profile-summary")]
        public async Task<IActionResult> GetProfileSummary()
        {
            var userId = _userContext.GetCurrentUserId().ToString() ?? throw new ArgumentException("No user id claim found");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            var isAdmin = (await _userManager.GetRolesAsync(user)).Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            var userProfile = await _userAccountService.GetUserProfileAsync(userId);
            var subscription = await _userAccountService.GetUserSubscriptionDetailsAsync(userId);

            var cardData = new { IsAdmin = isAdmin, UserProfile = userProfile, Subscription = subscription };
            return Ok(cardData);
        }
        
        [HttpGet("subscription-details")]
        public async Task<IActionResult> GetSubscriptionDetails()
        {
            var userId = _userContext.GetCurrentUserId().ToString() ?? throw new ArgumentException("No user id claim found");
            var subscription = await _userAccountService.GetUserSubscriptionDetailsAsync(userId);
            return Ok(subscription);
        }
        
        [HttpPut("subscription/status")]
        public async Task<IActionResult> UpdateSubscriptionStatus([FromBody] SubscriptionResponseDto dto)
        {
            try
            {
                var success = await _userAccountService.UpdateSubscriptionStatusAsync(dto.Status);
                if (!success)
                {
                    return NotFound(new { message = "Assinatura não encontrada ou falha ao atualizar o status." });
                }
                return Ok(new { message = $"Assinatura atualizada para '{dto.Status}' com sucesso." });
            }
            catch (AppServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no endpoint UpdateSubscriptionStatus.");
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }
        
        [HttpGet("cards")]
        public async Task<IActionResult> ListMyCards()
        {
            try
            {
                var cards = await _clientService.ListCardsFromCustomerAsync();
                return Ok(cards);
            }
            catch (AppServiceException ex)
            {
                _logger.LogError(ex, "Erro no provedor de pagamento ao listar cartões.");
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpDelete("cards/{cardId}")]
        public async Task<IActionResult> DeleteMyCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return BadRequest(new { message = "O ID do cartão é obrigatório." });
            }

            try
            {
                var deletedCard = await _clientService.DeleteCardFromCustomerAsync(cardId);
                return Ok(deletedCard);
            }
            catch (AppServiceException ex)
            {
                _logger.LogError(ex, "Erro do provedor de pagamento ao deletar o cartão {CardId}.", cardId);
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPut("subscription/card")]
        public async Task<IActionResult> ChangeSubscriptionCard([FromBody] CardRequestDto? request)
        {
            try
            {
                var success = await _userAccountService.UpdateSubscriptionCardAsync(request?.Token);
                if (!success)
                {
                    return NotFound(new { message = "Assinatura não encontrada ou falha ao atualizar." });
                }
                return Ok(new { message = "Cartão da assinatura atualizado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar o cartão da assinatura.");
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }
        
        [HttpGet("payment-history")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var userId = _userContext.GetCurrentUserId().ToString() ?? throw new ArgumentException("No user id claim found");
            var history = await _userAccountService.GetUserPaymentHistoryAsync(userId);
            return Ok(history);
        }

        [HttpPost("refunds/request")]
        public async Task<IActionResult> RequestRefund()
        {
            try
            {
                await _refundService.RequestUserRefundAsync();
                return Ok(new { message = "Solicitação de reembolso processada com sucesso." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Violação de regra de negócio ao solicitar reembolso.");
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de comunicação com a API de pagamento durante o reembolso.");
                return StatusCode(502, new { message = "Ocorreu um erro com nosso provedor de pagamento. Tente novamente mais tarde." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar a solicitação de reembolso.");
                return StatusCode(500, new { message = "Ocorreu um erro interno. Nossa equipe foi notificada." });
            }
        }
    }
}