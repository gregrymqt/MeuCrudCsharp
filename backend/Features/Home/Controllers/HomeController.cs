using System;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
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

    // =========================================================
    // LEITURA (PÚBLICA)
    // =========================================================

    /// <summary>
    /// Retorna todo o conteúdo da Home (Hero + Services).
    /// Aberto para qualquer usuário (não requer login).
    /// </summary>
    [HttpGet]
    [AllowAnonymous] // Sobrescreve o [Authorize] da ApiControllerBase 
    public async Task<IActionResult> GetHomeContent()
    {
        try
        {
            var content = await _service.GetHomeContentAsync();
            return Ok(content);
        }
        catch (Exception ex)
        {
            // Como não herdei de MercadoPagoApiControllerBase, replico a lógica de erro aqui
            return StatusCode(500, new { success = false, message = "Erro ao carregar a home.", error = ex.Message });
        }
    }

    // =========================================================
    // ESCRITA (RESTRITO - Requer Token JWT)
    // O [Authorize] da base class  protege estes métodos
    // =========================================================

    [HttpPost("hero")]
    public async Task<IActionResult> CreateHero([FromBody] HeroSlideDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateHeroAsync(dto);
            return CreatedAtAction(nameof(GetHomeContent), null, result);
        }
        catch (AppServiceException ex) // [cite: 2]
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao criar hero.", error = ex.Message });
        }
    }

    [HttpPut("hero/{id}")]
    public async Task<IActionResult> UpdateHero(int id, [FromBody] HeroSlideDto dto)
    {
        try
        {
            await _service.UpdateHeroAsync(id, dto);
            return NoContent();
        }
        catch (ResourceNotFoundException ex) // [cite: 4]
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao atualizar hero.", error = ex.Message });
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
        catch (ResourceNotFoundException ex) // [cite: 4]
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao deletar hero.", error = ex.Message });
        }
    }

    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] ServiceDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateServiceAsync(dto);
            return CreatedAtAction(nameof(GetHomeContent), null, result);
        }
        catch (AppServiceException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao criar serviço.", error = ex.Message });
        }
    }

    [HttpPut("services/{id}")]
    public async Task<IActionResult> UpdateService(int id, [FromBody] ServiceDto dto)
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao atualizar serviço.", error = ex.Message });
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
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Erro ao deletar serviço.", error = ex.Message });
        }
    }

}
