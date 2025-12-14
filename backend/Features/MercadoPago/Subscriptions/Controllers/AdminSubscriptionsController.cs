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

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminSubscriptionsController"/> class.
        /// </summary>
        /// <param name="subscriptionService">The service for subscription business logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public AdminSubscriptionsController(
            ISubscriptionService subscriptionService,
            ILogger<AdminSubscriptionsController> logger
        )
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Searches for a subscription by its unique identifier.
        /// </summary>
        /// <param name="query">The ID of the subscription to search for.</param>
        /// <returns>The details of the found subscription.</returns>
        /// <response code="200">Returns the subscription details.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        /// <response code="502">If there is a communication failure with the payment provider.</response>
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

        /// <summary>
        /// Updates the transaction amount of a specific subscription.
        /// </summary>
        /// <param name="id">The unique identifier of the subscription to update.</param>
        /// <param name="dto">The DTO containing the new transaction amount.</param>
        /// <returns>The updated subscription details.</returns>
        /// <response code="200">Returns the updated subscription details.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        /// <response code="502">If there is a communication failure with the payment provider.</response>
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

        /// <summary>
        /// Updates the status of a specific subscription (e.g., pause, reactivate).
        /// </summary>
        /// <param name="id">The unique identifier of the subscription to update.</param>
        /// <param name="dto">The DTO containing the new status.</param>
        /// <returns>The updated subscription details.</returns>
        /// <response code="200">Returns the updated subscription details.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        /// <response code="502">If there is a communication failure with the payment provider.</response>
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
