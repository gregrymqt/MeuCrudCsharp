using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/plans")]
    [Authorize(Roles = "Admin")]
    public class AdminPlansController : ControllerBase
    {
        private readonly IMercadoPagoService _mercadoPagoService;

        public AdminPlansController(IMercadoPagoService mercadoPagoService)
        {
            _mercadoPagoService = mercadoPagoService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto createPlanDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var planResponse = await _mercadoPagoService.CreatePlanAsync(createPlanDto);
                // Retorna 201 Created com o objeto de resposta
                return CreatedAtAction(
                    nameof(CreatePlan),
                    new { id = planResponse.Id },
                    planResponse
                );
            }
            catch (Exception ex)
            {
                // Em caso de erro na comunicação com a API, retorna um erro de servidor
                return StatusCode(
                    500,
                    new
                    {
                        message = "Ocorreu um erro interno ao se comunicar com o serviço de pagamento.",
                        error = ex.Message,
                    }
                );
            }
        }
    }
}
