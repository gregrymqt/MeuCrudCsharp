using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/subscriptions")]
    // [Authorize(Roles = "Admin")]
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
                var result = await _subscriptionService.SearchSubscriptionAsync(query);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/value")]
        public async Task<IActionResult> UpdateValue(string id, [FromBody] UpdateSubscriptionValueDto dto)
        {
            try
            {
                var result = await _subscriptionService.UpdateSubscriptionValueAsync(id, dto);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateSubscriptionStatusDto dto)
        {
            try
            {
                var result = await _subscriptionService.UpdateSubscriptionStatusAsync(id, dto);
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
