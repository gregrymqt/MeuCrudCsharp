// Features/Videos/DTOs/VideoDto.cs
using System;

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
}
