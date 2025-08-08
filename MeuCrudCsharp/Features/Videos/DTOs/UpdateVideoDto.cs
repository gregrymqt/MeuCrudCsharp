using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class UpdateVideoDto
    {
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }

        // --- PROPRIEDADE ADICIONADA ---
        public IFormFile? ThumbnailFile { get; set; }
    }
}
