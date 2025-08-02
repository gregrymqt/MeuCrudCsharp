using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Profiles.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/subscriptions")]
    [Authorize(Roles = "Admin")]
    public class AdminSubscriptionsController : ControllerBase
    {
        private readonly IMercadoPagoService _mercadoPagoService;
        private readonly ICacheService _cacheService;
        private readonly ApiDbContext _context;

        public AdminSubscriptionsController(
            IMercadoPagoService mercadoPagoService,
            ICacheService cacheService,
            ApiDbContext context
        )
        {
            _mercadoPagoService = mercadoPagoService;
            _cacheService = cacheService;
            _context = context;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            try
            {
                var result = await _mercadoPagoService.GetSubscriptionAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Usando StatusCode() para erros, que também serializa o objeto de erro.
                return StatusCode(500, new { message = ex.Message });
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
                var result = await _mercadoPagoService.UpdateSubscriptionValueAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
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
                var result = await _mercadoPagoService.UpdateSubscriptionStatusAsync(
                    id,
                    dto.Status
                );
                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.ExternalId == id
                );
                if (subscription != null)
                {
                    var userCacheKey = $"SubscriptionDetails_{subscription.UserId}";
                    await _cacheService.RemoveAsync(userCacheKey);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
