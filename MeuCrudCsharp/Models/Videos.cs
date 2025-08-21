using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    // Enum para controlar o estado do vídeo de forma segura e legível.
    public enum VideoStatus
    {
        Processing, // 0 - Em processamento (logo após o upload)
        Available, // 1 - Disponível para assistir
        Error, // 2 - Ocorreu um erro no processamento
    }

    [Index(nameof(CourseId))]
    public class Video
    {
        [Key]
        public int Id { get; set; }
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string Description { get; set; }
        public string StorageIdentifier { get; set; }
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public VideoStatus Status { get; set; }
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; }

        // NOVO: Adicione este campo
        [MaxLength(2048)] // Um bom tamanho para URLs
        public string? ThumbnailUrl { get; set; }

        public Video()
        {
            UploadDate = DateTime.UtcNow;
            Status = VideoStatus.Processing; // O vídeo sempre começa como "Processando"
        }
    }
}
