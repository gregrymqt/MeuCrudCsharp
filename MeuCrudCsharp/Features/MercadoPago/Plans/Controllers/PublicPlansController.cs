    using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Controllers
{
    /// <summary>
    /// Provides public, unauthenticated access to view subscription plans.
    /// </summary>
   [Route("api/public/plans")]
    public class PublicPlansController : ApiControllerBase
    {
        private readonly IPlanService _planService;
        private readonly ILogger<PublicPlansController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicPlansController"/> class.
        /// </summary>
        /// <param name="planService">The service responsible for plan business logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public PublicPlansController(
            IPlanService planService,
            ILogger<PublicPlansController> logger
        )
        {
            _planService = planService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all active subscription plans.
        /// </summary>
        /// <returns>A list of active plans available to the public.</returns>
        /// <response code="200">Returns the list of active plans.</response>
        /// <response code="500">If an unexpected server error occurs while fetching the plans.</response>
        // Exemplo para o Public Controller (aplique o mesmo para o Admin)
        [HttpGet]
        public async Task<IActionResult> GetPlans([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Passa os parâmetros para o serviço
                var plans = await _planService.GetActiveDbPlansAsync(page, pageSize);
                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching public plans.");
                return StatusCode(500, new { message = "Could not load plans at this time." });
            }
        }
    }
}
