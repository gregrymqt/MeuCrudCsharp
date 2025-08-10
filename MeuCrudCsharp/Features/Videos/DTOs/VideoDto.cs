using System;
using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    /// <summary>
    /// Represents the data for a video, suitable for display and transfer.
    /// </summary>
    public class VideoDto
    {
        /// <summary>
        /// The unique identifier for the video.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// The title of the video.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        /// <summary>
        /// A brief description of the video's content.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The unique identifier for the video's files in the storage system (e.g., a GUID for the folder).
        /// </summary>
        [Required]
        public string? StorageIdentifier { get; set; }

        /// <summary>
        /// The date and time when the video was originally uploaded.
        /// </summary>
        [Required]
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// The total duration of the video.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The current processing status of the video (e.g., "Processing", "Available", "Failed").
        /// </summary>
        [Required]
        public string? Status { get; set; }

        /// <summary>
        /// The name of the course this video belongs to.
        /// </summary>
        [Required]
        public string? CourseName { get; set; }

        /// <summary>
        /// The URL for the video's thumbnail image.
        /// </summary>
        [Url]
        public string? ThumbnailUrl { get; set; }
    }
}
