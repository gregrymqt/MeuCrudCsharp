using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Controllers
{
    /// <summary>
    /// Manages administrative operations for subscriptions, such as searching and updating.
    /// Requires 'Admin' role for access.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin/subscriptions")]
    public class AdminSubscriptionsController : MercadoPagoApiControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<AdminSubscriptionsController> _logger;

        public AdminSubscriptionsController(
            ISubscriptionService subscriptionService,
            ILogger<AdminSubscriptionsController> logger
        )
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            try
            {
                var result = await _subscriptionService.GetSubscriptionByIdAsync(query);
                return Ok(result);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "External API error while searching for subscription {SubscriptionId}.",
                    query
                );
                return StatusCode(502, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while searching for subscription {SubscriptionId}.",
                    query
                );
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPut("{id}/value")]
        [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateValue(
            string id,
            [FromBody] UpdateSubscriptionValueDto dto
        )
        {
            try
            {
                var result = await _subscriptionService.UpdateSubscriptionValueAsync(id, dto);
                return Ok(result);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "External API error while updating value for subscription {SubscriptionId}.",
                    id
                );
                return StatusCode(502, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating value for subscription {SubscriptionId}.",
                    id
                );
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus(
            string id,
            [FromBody] UpdateSubscriptionStatusDto dto
        )
        {
            try
            {
                var result = await _subscriptionService.UpdateSubscriptionStatusAsync(id, dto);
                return Ok(result);
            }
            catch (ExternalApiException ex)
            {
                _logger.LogError(
                    ex,
                    "External API error while updating status for subscription {SubscriptionId}.",
                    id
                );
                return StatusCode(502, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating status for subscription {SubscriptionId}.",
                    id
                );
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}
