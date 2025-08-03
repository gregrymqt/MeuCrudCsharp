using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class UpdateVideoDto
    {
        // ... propriedades existentes ...
        public string Title { get; set; }
        public string Description { get; set; }

        // NOVO
        [Url(ErrorMessage = "A URL da thumbnail n�o � v�lida.")]
        public string? ThumbnailUrl { get; set; }
    }
}
