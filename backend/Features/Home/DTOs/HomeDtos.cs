using System.Collections.Generic;
using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.Files.DTOs;
using Microsoft.AspNetCore.Http; // Necessário para IFormFile

namespace MeuCrudCsharp.Features.Home.DTOs;

// =================================================================
// DTOs DE LEITURA (SAÍDA - GET)
// =================================================================

public class HomeContentDto
{
    [JsonPropertyName("hero")]
    public List<HeroSlideDto> Hero { get; set; } = new();

    [JsonPropertyName("services")]
    public List<ServiceDto> Services { get; set; } = new();
}

public class HeroSlideDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}

public class ServiceDto
{
    public int Id { get; set; }
    public string IconClass { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}

// =================================================================
// DTOs DE ESCRITA (ENTRADA - POST/PUT)
// Devem ser usados com [FromForm] nos Controllers
// =================================================================

public class CreateUpdateHeroDto : BaseUploadDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}

public class CreateUpdateServiceDto
{
    public string IconClass { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}
