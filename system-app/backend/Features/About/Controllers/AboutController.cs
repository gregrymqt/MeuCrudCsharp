using MeuCrudCsharp.Features.About.DTOs;
using MeuCrudCsharp.Features.About.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.About.Controllers;

[Route("api/[controller]")]
public class AboutController : ApiControllerBase
{
    private readonly IAboutService _service;

    public AboutController(IAboutService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAboutPageContent()
    {
        try
        {
            var content = await _service.GetAboutPageContentAsync();
            return Ok(content);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new
                {
                    success = false,
                    message = "Erro ao carregar a página Sobre.",
                    error = ex.Message,
                }
            );
        }
    }

    // ==========================================
    // SEÇÕES (Upload de Imagem)
    // ==========================================

    [HttpPost("sections")]
    [AllowLargeFile(2048)]
    public async Task<IActionResult> CreateSection([FromForm] CreateUpdateAboutSectionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // O resultado pode ser NULL se for um chunk intermediário
            var result = await _service.CreateSectionAsync(dto);

            if (result == null)
            {
                // Chunk recebido com sucesso, mande o próximo!
                return Ok(new { message = $"Chunk {dto.ChunkIndex} recebido." });
            }

            // Upload completo e registro criado
            return CreatedAtAction(nameof(GetAboutPageContent), null, result); // Ajuste o nameof conforme sua rota de GET
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("sections/{id}")]
    [AllowLargeFile(2048)]
    public async Task<IActionResult> UpdateSection(
        int id,
        [FromForm] CreateUpdateAboutSectionDto dto
    )
    {
        try
        {
            bool finished = await _service.UpdateSectionAsync(id, dto);

            if (!finished)
            {
                return Ok(new { message = $"Chunk {dto.ChunkIndex} atualizado." });
            }

            return NoContent(); // Update concluído com sucesso
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

    [HttpDelete("sections/{id}")]
    public async Task<IActionResult> DeleteSection(int id)
    {
        try
        {
            await _service.DeleteSectionAsync(id);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    // ==========================================
    // EQUIPE (Upload de Foto)
    // ==========================================

    [HttpPost("team")]
    [AllowLargeFile(2048)]
    public async Task<IActionResult> CreateTeamMember([FromForm] CreateUpdateTeamMemberDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateTeamMemberAsync(dto);

            if (result == null)
            {
                return Ok(new { message = $"Chunk {dto.ChunkIndex} recebido." });
            }

            return CreatedAtAction(nameof(GetAboutPageContent), null, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("team/{id}")]
    [AllowLargeFile(2048)] // Permite upload de arquivos até 2GB
    public async Task<IActionResult> UpdateTeamMember(
        int id,
        [FromForm] CreateUpdateTeamMemberDto dto
    )
    {
        try
        {
            bool finished = await _service.UpdateTeamMemberAsync(id, dto);
            if (finished)
            {
                return Ok(new { message = $"Chunk {dto.ChunkIndex} atualizado." });
            }

            return NoContent();
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

    [HttpDelete("team/{id}")]
    public async Task<IActionResult> DeleteTeamMember(int id)
    {
        try
        {
            await _service.DeleteTeamMemberAsync(id);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }
}
