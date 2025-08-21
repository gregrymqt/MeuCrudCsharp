using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Course.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) para a atualização de um curso existente.
    /// </summary>
    public class UpdateCourseDto
    {
        /// <summary>
        /// O novo nome do curso. Deve ter entre 3 e 100 caracteres.
        /// </summary>
        /// <example>Curso de C# para Web com ASP.NET Core</example>
        [Required(ErrorMessage = "O nome do curso é obrigatório.")]
        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }

        /// <summary>
        /// A nova descrição opcional para o curso. Máximo de 500 caracteres.
        /// </summary>
        /// <example>Aprenda a construir aplicações web robustas com o framework ASP.NET Core.</example>
        [StringLength(500)]
        public string? Description { get; set; }
    }
}
