using MeuCrudCsharp.Features.About.DTOs;
using MeuCrudCsharp.Features.About.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
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
    public async Task<IActionResult> CreateSection([FromForm] CreateUpdateAboutSectionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateSectionAsync(dto);
            return CreatedAtAction(nameof(GetAboutPageContent), null, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("sections/{id}")]
    public async Task<IActionResult> UpdateSection(
        int id,
        [FromForm] CreateUpdateAboutSectionDto dto
    )
    {
        try
        {
            await _service.UpdateSectionAsync(id, dto);
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
    public async Task<IActionResult> CreateTeamMember([FromForm] CreateUpdateTeamMemberDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateTeamMemberAsync(dto);
            return CreatedAtAction(nameof(GetAboutPageContent), null, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("team/{id}")]
    public async Task<IActionResult> UpdateTeamMember(
        int id,
        [FromForm] CreateUpdateTeamMemberDto dto
    )
    {
        try
        {
            await _service.UpdateTeamMemberAsync(id, dto);
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
