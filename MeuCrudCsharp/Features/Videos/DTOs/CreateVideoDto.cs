using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class CreateVideoDto
    {
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }

        [Required]
        public string StorageIdentifier { get; set; }

        [Required]
        public string CourseName { get; set; }

        // --- PROPRIEDADE ADICIONADA ---
        public IFormFile? ThumbnailFile { get; set; }
    }
}
