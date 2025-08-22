using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.Interfaces;

namespace MeuCrudCsharp.Features.Videos.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<string?> SaveThumbnailAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            var thumbnailsDirectory = Path.Combine(_env.WebRootPath, "thumbnails");
            Directory.CreateDirectory(thumbnailsDirectory);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(thumbnailsDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/thumbnails/{fileName}";
        }

        public Task DeleteThumbnailAsync(string? thumbnailUrl)
        {
            if (string.IsNullOrEmpty(thumbnailUrl))
                return Task.CompletedTask;

            try
            {
                var relativePath = thumbnailUrl
                    .TrimStart('/')
                    .Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_env.WebRootPath, relativePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation(
                        "Old thumbnail '{Path}' deleted successfully.",
                        fullPath
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete old thumbnail for URL: {Url}",
                    thumbnailUrl
                );
                // Não relançamos a exceção para não quebrar a operação principal (ex: Update)
            }
            return Task.CompletedTask;
        }

        public Task DeleteVideoAssetsAsync(string storageIdentifier)
        {
            try
            {
                var videoFolderPath = Path.Combine(_env.WebRootPath, "Videos", storageIdentifier);
                if (Directory.Exists(videoFolderPath))
                {
                    Directory.Delete(videoFolderPath, recursive: true);
                    _logger.LogInformation(
                        "Video assets folder '{Path}' deleted.",
                        videoFolderPath
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete assets for video identifier {Id}.",
                    storageIdentifier
                );
                throw new AppServiceException("Error removing physical video files.", ex);
            }
            return Task.CompletedTask;
        }
    }
}
