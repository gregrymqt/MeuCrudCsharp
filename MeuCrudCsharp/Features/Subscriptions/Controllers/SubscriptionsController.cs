using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Subscriptions.Controllers
{
    /// <summary>
    /// Manages the creation of new subscriptions for authenticated users.
    /// </summary>
    public class SubscriptionsController : ApiControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsController"/> class.
        /// </summary>
        /// <param name="subscriptionService">The service for subscription business logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public SubscriptionsController(
            ISubscriptionService subscriptionService,
            ILogger<SubscriptionsController> logger
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
                        createDto,
                        User
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
