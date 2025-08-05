using MeuCrudCsharp.Features.Plans.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Plans.Controllers
{
    [ApiController]
    [Route("api/plans")]
    public class PublicPlansController : ControllerBase
    {
        private readonly IPlanService _planService;

        public PublicPlansController(IPlanService planService)
        {
            _planService = planService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var plans = await _planService.GetActivePlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                // Logar o erro `ex`
                return StatusCode(
                    500,
                    new { message = "Não foi possível carregar os planos no momento." }
                );
            }
        }
    }
}
