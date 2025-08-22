using System;
using System.Collections.Generic;
using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Course.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) que representa um curso para exibição.
    /// Usado para retornar dados de um curso, incluindo seus vídeos associados.
    /// </summary>
    public class CourseDto
    {
        public Guid Id { get; set; } // Representa o PublicId
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<VideoDto> Videos { get; set; } = new();
    }
}
