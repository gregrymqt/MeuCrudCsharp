// Features/Videos/DTOs/VideoDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using MeuCrudCsharp.Features.Files.DTOs;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    public class VideoDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StorageIdentifier { get; set; }
        public DateTime UploadDate { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; }
        public string CourseName { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class UpdateVideoDto
    {
        /// <summary>
        /// The new title for the video.
        /// </summary>
        [Required(ErrorMessage = "The title is required.")]
        [StringLength(200, ErrorMessage = "The title cannot be longer than 200 characters.")]
        public string? Title { get; set; }

        /// <summary>
        /// The new description for the video.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// An optional new thumbnail file to replace the existing one.
        /// If not provided, the current thumbnail will be kept.
        /// </summary>
        public IFormFile? ThumbnailFile { get; set; }
    }

    public record PaginatedResultDto<T>(List<T> Items, int TotalCount, int Page, int PageSize);

    public class CreateVideoDto : BaseUploadDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // CORREÇÃO: Mudado de Guid para int para bater com a Model Video.CourseId
        [Required]
        public int CourseId { get; set; }

        public IFormFile? ThumbnailFile { get; set; }
    }
}
