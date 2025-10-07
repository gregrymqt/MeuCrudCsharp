
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.Controllers
{
    /// <summary>
    /// Manages the creation of new subscriptions for authenticated users.
    /// </summary>
    [Route("api/subscriptions/public")]
    public class SubscriptionsPublicController : ApiControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionsPublicController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsPublicController"/> class.
        /// </summary>
        /// <param name="subscriptionService">The service for subscription business logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public SubscriptionsPublicController(
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionsPublicController> logger
        )
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new subscription for the authenticated user.
        /// If the user does not yet exist as a customer in the payment provider, one will be created.
        /// </summary>
        /// <param name="createDto">The DTO containing the plan and payment details for the new subscription.</param>
        /// <returns>The details of the newly created subscription.</returns>
        /// <response code="200">Returns the created subscription details.</response>
        /// <response code="400">If the request data is invalid or a business rule is violated.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPost]
        [ProducesResponseType(typeof(SubscriptionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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
                var subscriptionResponse =
                    await _subscriptionService.CreateSubscriptionAndCustomerIfNeededAsync(
                        createDto
                    );
                return Ok(subscriptionResponse);
            }
            catch (AppServiceException ex)
            {
                _logger.LogWarning(
                    ex,
                    "A business logic error occurred while creating a subscription."
                );
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating a subscription.");
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}
