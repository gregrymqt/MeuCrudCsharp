using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class CreateVideoDto
    {
        // ... propriedades existentes ...
        public string Title { get; set; }
        public string Description { get; set; }
        public string StorageIdentifier { get; set; }
        public string CourseName { get; set; }

        // NOVO
        [Url(ErrorMessage = "A URL da thumbnail não é válida.")]
        public string? ThumbnailUrl { get; set; }
    }
}
