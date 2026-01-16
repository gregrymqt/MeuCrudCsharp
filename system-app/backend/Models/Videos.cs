using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// Certifique-se de que o namespace da EntityFile esteja acessível aqui
// using MeuCrudCsharp.Domain.Models;

namespace MeuCrudCsharp.Models
{
    public enum VideoStatus
    {
        Processing, // 0
        Available, // 1
        Error, // 2
    }

    [Index(nameof(CourseId))]
    [Index(nameof(PublicId), IsUnique = true)]
    public class Video
    {
        [Key]
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required] // É bom marcar como obrigatório
        public string Title { get; set; }

        public string Description { get; set; }

        // Identificador usado para a pasta de streaming (HLS)
        public string StorageIdentifier { get; set; }

        public DateTime UploadDate { get; set; }

        public TimeSpan Duration { get; set; }

        public VideoStatus Status { get; set; }

        // --- RELACIONAMENTOS ---

        // 1. Relacionamento com Curso (Existente)
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        // Isso vincula o vídeo ao arquivo salvo em "uploads/Videos/..."
        public int FileId { get; set; }

        [ForeignKey("FileId")]
        public virtual EntityFile File { get; set; } // Propriedade de navegação

        // -----------------------

        [MaxLength(2048)]
        public string? ThumbnailUrl { get; set; }

        public Video()
        {
            UploadDate = DateTime.UtcNow;
            Status = VideoStatus.Processing;
        }
    }
}
