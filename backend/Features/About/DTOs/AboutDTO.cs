using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.Files.DTOs;

namespace MeuCrudCsharp.Features.About.DTOs;

/// <summary>
/// Enum para controlar o tipo de conteúdo no C#.
/// Ref: types/AboutTypes.ts [AboutContentType]
/// </summary>
public enum AboutContentTypeDto
{
    section1, // Texto + Imagem
    section2, // Time/Membros
}

/// <summary>
/// DTO para a Seção Genérica (Texto + Imagem)
/// Ref: types/AboutTypes.ts [AboutSectionData]
/// </summary>
public class AboutSectionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    // Converte o Enum para string (ex: "section1") na serialização se necessário,
    // ou você pode enviar como string direto.
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "section1";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("imageAlt")]
    public string ImageAlt { get; set; } = string.Empty;
}

/// <summary>
/// DTO para a Seção de Equipe
/// Ref: types/AboutTypes.ts [AboutTeamData]
/// </summary>
public class AboutTeamSectionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "section2";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("members")]
    public List<TeamMemberDto> Members { get; set; } = new();
}

/// <summary>
/// DTO dos Membros da equipe
/// Ref: types/AboutTypes.ts [TeamMember]
/// </summary>
public class TeamMemberDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;

    [JsonPropertyName("linkedinUrl")]
    public string? LinkedinUrl { get; set; }

    [JsonPropertyName("githubUrl")]
    public string? GithubUrl { get; set; }
}

public class CreateUpdateAboutSectionDto : BaseUploadDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string ImageAlt { get; set; } = string.Empty;

    public int OrderIndex { get; set; } = 0;
}

public class CreateUpdateTeamMemberDto : BaseUploadDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string? LinkedinUrl { get; set; }

    [Required]
    public string? GithubUrl { get; set; }
}

public class AboutPageContentDto
{
    public List<AboutSectionDto> Sections { get; set; } = new();
    public AboutTeamSectionDto TeamSection { get; set; } = new();
}
