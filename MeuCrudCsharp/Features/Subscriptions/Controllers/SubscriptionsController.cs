using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Subscriptions.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
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
                var subscriptionResponse =
                    await _subscriptionService.CreateSubscriptionAndCustomerIfNeededAsync(
                        createDto,
                        User
                    );
                return Ok(subscriptionResponse);
            }
            catch (AppServiceException ex)
            {
                return StatusCode(
                    500,
                    new { message = ex.Message, error = ex.InnerException?.Message }
                );
            }
        }
    }
}
