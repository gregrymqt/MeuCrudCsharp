using System;
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

    // =========================================================
    // LEITURA (PÚBLICA)
    // =========================================================

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

    // =========================================================
    // HERO - REQUER UPLOAD DE ARQUIVO (FORMDATA)
    // =========================================================

    [HttpPost("hero")]
    [AllowLargeFile(2048)] // Permite upload de arquivos até 2GB
    // Mudamos para [FromForm] para aceitar arquivo + texto
    // Usamos o DTO de escrita CreateUpdateHeroDto
    public async Task<IActionResult> CreateHero([FromForm] CreateUpdateHeroDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _service.CreateHeroAsync(dto);
            // Retorna 201 Created
            return CreatedAtAction(nameof(GetHomeContent), null, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPut("hero/{id}")]
    [AllowLargeFile(2048)] // Permite upload de arquivos até 2GB
    // Mudamos para [FromForm] aqui também
    public async Task<IActionResult> UpdateHero(int id, [FromForm] CreateUpdateHeroDto dto)
    {
        try
        {
            await _service.UpdateHeroAsync(id, dto);
            return NoContent();
        }
        catch (ResourceNotFoundException ex) //
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
        catch (ResourceNotFoundException ex) //
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    // =========================================================
    // SERVICES - APENAS TEXTO (JSON)
    // =========================================================

    [HttpPost("services")]
    // Mantemos [FromBody] pois ServiceDto não tem arquivo (apenas strings)
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
