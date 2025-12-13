using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Courses.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) para a criação de um novo curso.
    /// Contém os dados necessários para registrar um curso no sistema.
    /// </summary>
    public class CreateCourseDto
    {
        /// <summary>
        /// O nome do curso. Deve ter entre 3 e 100 caracteres.
        /// </summary>
        /// <example>Curso de C# Avançado</example>
        [Required(ErrorMessage = "O nome do curso é obrigatório.")]
        [StringLength(100, MinimumLength = 3)]
        public string? Name { get; set; }

        /// <summary>
        /// Uma descrição opcional sobre o conteúdo do curso. Máximo de 500 caracteres.
        /// </summary>
        /// <example>Neste curso, você aprenderá sobre tópicos avançados em C#...</example>
        [StringLength(500)]
        public string? Description { get; set; }
    }
}
