using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class CreateVideoDto
    {
        [Required]
        public string? Title { get; set; }
        public string? Description { get; set; }

        [Required]
        public string? StorageIdentifier { get; set; } // O GUID retornado pelo UploadController

        [Required]
        public string? CourseName { get; set; } // O nome do curso para associar
    }
}
