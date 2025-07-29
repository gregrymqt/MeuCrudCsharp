using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    // Enum para controlar o estado do vídeo de forma segura e legível.
    public enum VideoStatus
    {
        Processing, // 0 - Em processamento (logo após o upload)
        Available, // 1 - Disponível para assistir
        Error, // 2 - Ocorreu um erro no processamento
    }

    public class Video
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // 1. Armazena apenas o nome da pasta (GUID). Muito mais robusto.
        [Required]
        public string StorageIdentifier { get; set; } = string.Empty;

        [Required]
        public DateTime UploadDate { get; set; }

        // Duração do vídeo. Pode ser preenchida após o processamento com FFmpeg.
        public TimeSpan Duration { get; set; }

        // 2. Campo para saber o estado do vídeo.
        [Required]
        public VideoStatus Status { get; set; }

        // 3. Exemplo de relacionamento: Este vídeo pertence a um curso.
        [Required]
        public Guid CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        public Video()
        {
            Id = Guid.NewGuid();
            UploadDate = DateTime.UtcNow;
            Status = VideoStatus.Processing; // O vídeo sempre começa como "Processando"
        }
    }
}
