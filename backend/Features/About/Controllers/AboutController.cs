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

    // =========================================================
    // LEITURA (PÚBLICA)
    // =========================================================

    /// <summary>
    /// Retorna todo o conteúdo da página About (Seções + Time).
    /// Aberto para qualquer usuário.
    /// </summary>
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
            return StatusCode(500, new { success = false, message = "Erro ao carregar a página Sobre.", error = ex.Message });
        }
    }

    // =========================================================
    // ESCRITA - GENERIC SECTIONS (RESTRITO)
    // =========================================================

    [HttpPost("sections")]
    public async Task<IActionResult> CreateSection([FromBody] AboutSectionDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateSectionAsync(dto);
            // Retorna 201 Created
            return CreatedAtAction(nameof(GetAboutPageContent), null, result);
        }
        catch (AppServiceException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao criar seção.", error = ex.Message });
        }
    }

    [HttpPut("sections/{id}")]
    public async Task<IActionResult> UpdateSection(int id, [FromBody] AboutSectionDto dto)
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
            return StatusCode(500, new { success = false, message = "Erro ao atualizar seção.", error = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao deletar seção.", error = ex.Message });
        }
    }

    // =========================================================
    // ESCRITA - TEAM MEMBERS (RESTRITO)
    // =========================================================

    [HttpPost("team")]
    public async Task<IActionResult> CreateTeamMember([FromBody] TeamMemberDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateTeamMemberAsync(dto);
            return CreatedAtAction(nameof(GetAboutPageContent), null, result);
        }
        catch (AppServiceException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao adicionar membro.", error = ex.Message });
        }
    }

    [HttpPut("team/{id}")]
    public async Task<IActionResult> UpdateTeamMember(int id, [FromBody] TeamMemberDto dto)
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
            return StatusCode(500, new { success = false, message = "Erro ao atualizar membro.", error = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao remover membro.", error = ex.Message });
        }
    }
}
