using System;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Home.DTOs;

// ==========================================
    // 1. HOME PAGE DTOs
    // ==========================================

    /// <summary>
    /// Representa o objeto final que o Hook do Front recebe para a Home.
    /// Ref: types/home.types.ts [HomeContent]
    /// </summary>
    public class HomeContentDto
    {
        [JsonPropertyName("hero")]
        public List<HeroSlideDto> Hero { get; set; } = new();

        [JsonPropertyName("services")]
        public List<ServiceDto> Services { get; set; } = new();
    }

    /// <summary>
    /// DTO para o Carrossel/Hero Principal
    /// Ref: types/home.types.ts [HeroSlideData]
    /// </summary>
    public class HeroSlideDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; } = string.Empty;

        [JsonPropertyName("actionText")]
        public string ActionText { get; set; } = string.Empty;

        [JsonPropertyName("actionUrl")]
        public string ActionUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para a seção de Serviços/Features
    /// Ref: types/home.types.ts [ServiceData]
    /// </summary>
    public class ServiceDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("iconClass")]
        public string IconClass { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("actionText")]
        public string ActionText { get; set; } = string.Empty;

        [JsonPropertyName("actionUrl")]
        public string ActionUrl { get; set; } = string.Empty;
    }
