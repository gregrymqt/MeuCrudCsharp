using System.Security.Claims;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions; // Importante para ResourceNotFoundException
using MeuCrudCsharp.Features.Support.DTOs;
using MeuCrudCsharp.Features.Support.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Support.Controllers
{
    [Route("api/support")]
    public class SupportController : ApiControllerBase
    {
        private readonly ISupportService _service;
        private readonly ILogger<SupportController> _logger;

        public SupportController(ISupportService service, ILogger<SupportController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupportTicketDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(
                        new { success = false, message = "Usuário não identificado." }
                    );

                await _service.CreateTicketAsync(userId, dto);
                return Created(
                    "",
                    new { success = true, message = "Ticket de suporte criado com sucesso." }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar ticket.");
                return StatusCode(
                    500,
                    new { success = false, message = "Erro ao processar sua solicitação." }
                );
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            try
            {
                var result = await _service.GetAllTicketsPaginatedAsync(page, pageSize);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar tickets.");
                return StatusCode(500, new { success = false, message = "Erro interno." });
            }
        }

        // --- NOVO: BUSCA POR ID DO TICKET ---
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")] // Apenas Admin vê detalhes por enquanto
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var ticket = await _service.GetTicketByIdAsync(id);
                return Ok(new { success = true, data = ticket });
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar ticket {id}.");
                return StatusCode(500, new { success = false, message = "Erro interno." });
            }
        }

        // ------------------------------------

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(
            string id,
            [FromBody] UpdateSupportTicketDto dto
        )
        {
            try
            {
                await _service.UpdateTicketStatusAsync(id, dto);
                return Ok(new { success = true, message = "Status atualizado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar ticket.");
                if (ex.Message.Contains("não encontrado"))
                    return NotFound(new { success = false, message = ex.Message });

                return StatusCode(500, new { success = false, message = "Erro interno." });
            }
        }
    }
}
