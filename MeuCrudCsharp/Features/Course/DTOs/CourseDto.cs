using System;
using System.Collections.Generic;
using MeuCrudCsharp.Features.Videos.DTOs; // Reutilizando o VideoDto

namespace MeuCrudCsharp.Features.Courses.DTOs
{
    public class CourseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<VideoDto> Videos { get; set; } = new List<VideoDto>();
    }
}