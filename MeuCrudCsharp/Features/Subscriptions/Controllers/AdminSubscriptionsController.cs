using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Subscriptions.Controllers
{
    [ApiController]
    [Route("api/admin/subscriptions")]
    [Authorize(Roles = "Admin")]
    public class AdminSubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public AdminSubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            try
            {
                var result = await _subscriptionService.GetSubscriptionByIdAsync(query);
                return Ok(result);
            }
            catch (ExternalApiException ex)
            {
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/value")]
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
                return StatusCode(502, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
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
                return StatusCode(502, new { message = ex.Message });
            }
        }
    }
}
