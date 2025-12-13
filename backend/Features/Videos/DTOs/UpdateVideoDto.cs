using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MeuCrudCsharp.Features.Videos.DTOs
{
    /// <summary>
    /// Represents the data required to update an existing video's metadata and optionally its thumbnail.
    /// </summary>
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
}
