using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class VideoDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? StorageIdentifier { get; set; }
        public DateTime UploadDate { get; set; }
        [JsonIgnore] // Ignora a propriedade original TimeSpan na serialização
        public TimeSpan Duration { get; set; }

        // NOVO: Envia a duração total em segundos, que é fácil de usar no JS
        public double DurationInSeconds => Duration.TotalSeconds;
        public string? Status { get; set; }
        public string? CourseName { get; set; }

        public string? ThumbnailUrl { get; set; }
    }
}
