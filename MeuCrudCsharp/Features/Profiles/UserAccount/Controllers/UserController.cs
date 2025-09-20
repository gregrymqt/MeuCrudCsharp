using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Controllers
{
    /// <summary>
    /// Manages the authenticated user's subscription actions, such as changing payment methods,
    /// cancelling, and reactivating.
    /// </summary>
    [Route("api/user")]
    public class UserController : ApiControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ILogger<UserController> _logger;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionController"/> class.
        /// </summary>
        /// <param name="userAccountService">The service for user account and subscription logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public UserController(
            IUserAccountService userAccountService,
            ILogger<UserController> logger,
            IUserContext userContext
        )
        {
            _userAccountService = userAccountService;
            _logger = logger;
            _userContext = userContext;
        }

        /// <summary>
        /// Updates the payment card associated with the user's current subscription.
        /// </summary>
        /// <param name="request">A DTO containing the new card token from the payment provider.</param>
        /// <returns>A confirmation message upon success.</returns>
        /// <response code="200">Indicates the card was successfully updated.</response>
        /// <response code="400">If the request is invalid or the card token is missing.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the user's subscription is not found or the update fails.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPut("card")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeCard([FromBody] CardRequestDto? request)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var success = await _userAccountService.UpdateSubscriptionCardAsync(
                    userId,
                    request?.Token
                );

                if (!success)
                {
                    return NotFound(new { message = "Subscription not found or update failed." });
                }

                return Ok(new { message = "Subscription card updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing subscription card for user.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Endpoint para atualizar o status da assinatura do usuário (pausar, reativar, cancelar).
        /// </summary>
        [HttpPut("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus([FromBody] SubscriptionResponseDto dto)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var success = await _userAccountService.UpdateSubscriptionStatusAsync(userId, dto.Status);

                if (!success)
                {
                    // Pode ser que a assinatura não foi encontrada ou o MP não confirmou.
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
                _logger.LogError(ex, "Erro inesperado no endpoint UpdateStatus.");
                return StatusCode(500, new { message = "Ocorreu um erro inesperado." });
            }
        }
    }
}
