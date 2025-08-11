using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.UserAccount.Controllers
{
    /// <summary>
    /// Manages the authenticated user's subscription actions, such as changing payment methods,
    /// cancelling, and reactivating.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/user/subscription")]
    public class SubscriptionApiController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ILogger<SubscriptionApiController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionApiController"/> class.
        /// </summary>
        /// <param name="userAccountService">The service for user account and subscription logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public SubscriptionApiController(
            IUserAccountService userAccountService,
            ILogger<SubscriptionApiController> logger
        )
        {
            _userAccountService = userAccountService;
            _logger = logger;
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
        public async Task<IActionResult> ChangeCard([FromBody] UpdateCardTokenDto? request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userAccountService.UpdateSubscriptionCardAsync(
                    userId,
                    request?.NewCardToken
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
        /// Cancels the authenticated user's active subscription.
        /// </summary>
        /// <returns>A confirmation message upon success.</returns>
        /// <response code="200">Indicates the subscription was successfully cancelled.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the subscription is not found or is already cancelled.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelSubscription()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userAccountService.CancelSubscriptionAsync(userId);

                if (!success)
                {
                    return NotFound(
                        new { message = "Subscription not found or is already cancelled." }
                    );
                }

                return Ok(new { message = "Subscription cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription for user.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Reactivates the authenticated user's previously cancelled subscription.
        /// </summary>
        /// <returns>A confirmation message upon success.</returns>
        /// <response code="200">Indicates the subscription was successfully reactivated.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the subscription is not found or cannot be reactivated.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPost("reactivate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReactivateSubscription()
        {
            try
            {
                var userId = GetCurrentUserId();
                var success = await _userAccountService.ReactivateSubscriptionAsync(userId);

                if (!success)
                {
                    return NotFound(
                        new
                        {
                            message = "Subscription not found or is not in a state that allows reactivation.",
                        }
                    );
                }

                return Ok(new { message = "Subscription reactivated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating subscription for user.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Safely retrieves the current user's unique identifier from the security claims.
        /// </summary>
        /// <returns>The GUID of the authenticated user.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the user's identifier claim is missing or invalid.</exception>
        private string GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                throw new InvalidOperationException("Could not retrieve the user's identifier.");
            }

            return userIdString;
        }
    }
}
