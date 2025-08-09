using System;
using System.Collections.Generic;
using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Courses.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) que representa um curso para exibição.
    /// Usado para retornar dados de um curso, incluindo seus vídeos associados.
    /// </summary>
    public class CourseDto
    {
        /// <summary>
        /// O identificador único do curso.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// O nome do curso.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// A descrição do curso.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// A lista de vídeos que pertencem a este curso.
        /// </summary>
        public List<VideoDto> Videos { get; set; } = new List<VideoDto>();
    }
}
