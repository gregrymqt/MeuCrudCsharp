using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Attributes;
using MeuCrudCsharp.Features.Home.DTOs;
using MeuCrudCsharp.Features.Home.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Home.Controllers;

[Route("api/[controller]")]
public class HomeController : ApiControllerBase
{
    private readonly IHomeService _service;

    public HomeController(IHomeService service)
    {
        _service = service;
    }

    // ==========================================
    // LEITURA (GET)
    // ==========================================
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetHomeContent()
    {
        try
        {
            var content = await _service.GetHomeContentAsync();
            return Ok(content);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    success = false,
                    message = "Erro ao carregar a home.",
                    error = ex.Message,
                }
            );
        }
    }

    // ==========================================
    // HERO (COM UPLOAD E CHUNKS)
    // ==========================================

    [HttpPost("hero")]
    [AllowLargeFile(2048)]
    public async Task<IActionResult> CreateHero([FromForm] CreateUpdateHeroDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // O retorno será NULL se for um chunk intermediário
            var result = await _service.CreateHeroAsync(dto);

            if (result == null)
            {
                // Responde 200 OK para o React mandar o próximo pedaço
                return Ok(new { message = $"Chunk {dto.ChunkIndex} do Hero recebido." });
            }

            // Upload terminou e registro foi criado
            return CreatedAtAction(nameof(GetHomeContent), null, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("hero/{id}")]
    [AllowLargeFile(2048)]
    public async Task<IActionResult> UpdateHero(int id, [FromForm] CreateUpdateHeroDto dto)
    {
        try
        {
            // O retorno será FALSE se for um chunk intermediário
            bool finished = await _service.UpdateHeroAsync(id, dto);

            if (!finished)
            {
                return Ok(new { message = $"Chunk {dto.ChunkIndex} do Hero atualizado." });
            }

            return NoContent(); // 204: Tudo pronto
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("hero/{id}")]
    public async Task<IActionResult> DeleteHero(int id)
    {
        try
        {
            await _service.DeleteHeroAsync(id);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    // ==========================================
    // SERVICES (JSON PURO - SEM CHUNKS)
    // ==========================================

    // Mantemos [FromBody] e removemos a herança de BaseUploadDto na classe DTO
    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] CreateUpdateServiceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateServiceAsync(dto);
            return CreatedAtAction(nameof(GetHomeContent), null, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("services/{id}")]
    public async Task<IActionResult> UpdateService(int id, [FromBody] CreateUpdateServiceDto dto)
    {
        try
        {
            await _service.UpdateServiceAsync(id, dto);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("services/{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        try
        {
            await _service.DeleteServiceAsync(id);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
