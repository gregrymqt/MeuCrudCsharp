using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Courses.DTOs
{
    public class CreateCourseDto
    {
        [Required(ErrorMessage = "O nome do curso é obrigatório.")]
        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}