using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Service
{
    /// <summary>
    /// Implements <see cref="IVideoProcessingService"/> to handle video file conversions using FFmpeg.
    /// This service is designed to be run in the background (e.g., by Hangfire).
    /// </summary>
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ILogger<VideoProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string _ffmpegPath = "ffmpeg";
        private const string _ffprobePath = "ffprobe";

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoProcessingService"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording events and errors.</param>
        /// <param name="serviceProvider">The service provider to create dependency scopes for background jobs.</param>
        public VideoProcessingService(
            ILogger<VideoProcessingService> logger,
            IServiceProvider serviceProvider
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method creates a new dependency scope to safely interact with the database from a background thread.
        /// It updates the video's status to 'Available' on success or 'Error' on failure.
        /// </remarks>
        public async Task ProcessVideoToHlsAsync(
            string inputFilePath,
            string outputDirectory,
            string storageIdentifier
        )
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            Video? video = null;

            try
            {
                video = await context.Videos.FirstOrDefaultAsync(v =>
                    v.StorageIdentifier == storageIdentifier
                );
                if (video == null)
                {
                    throw new ResourceNotFoundException(
                        $"Video with StorageIdentifier {storageIdentifier} not found for processing."
                    );
                }

                if (!File.Exists(inputFilePath))
                {
                    throw new FileNotFoundException($"Input file not found: {inputFilePath}");
                }

                Directory.CreateDirectory(outputDirectory);

                var duration = await GetVideoDurationAsync(inputFilePath);

                var manifestPath = Path.Combine(outputDirectory, "manifest.m3u8");
                var arguments =
                    $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{Path.Combine(outputDirectory, "segment%03d.ts")}\" \"{manifestPath}\"";

                await RunProcessAsync(_ffmpegPath, arguments);

                video.Status = VideoStatus.Available;
                video.Duration = duration;
                _logger.LogInformation(
                    "Video {StorageIdentifier} processed successfully.",
                    storageIdentifier
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Critical failure while processing video {StorageIdentifier}.",
                    storageIdentifier
                );

                if (video != null)
                {
                    video.Status = VideoStatus.Error;
                }

                throw;
            }
            finally
            {
                if (video != null && context.ChangeTracker.HasChanges())
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Executes an external command-line process asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the executable file (e.g., "ffmpeg").</param>
        /// <param name="arguments">The command-line arguments to pass to the process.</param>
        /// <returns>A tuple containing the standard output and standard error streams as strings.</returns>
        /// <exception cref="AppServiceException">Thrown if the process fails to start or returns a non-zero exit code.</exception>
        private async Task<(string StandardOutput, string StandardError)> RunProcessAsync(
            string filePath,
            string arguments
        )
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new AppServiceException($"Could not start the process for '{filePath}'.");
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new AppServiceException(
                        $"Process '{filePath}' failed with exit code {process.ExitCode}. Error: {error}"
                    );
                }

                return (output, error);
            }
            catch (Win32Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error starting process '{FileName}'. Verify that FFmpeg/FFprobe is installed and in the system's PATH.",
                    filePath
                );
                throw new AppServiceException(
                    $"External dependency '{filePath}' not found. Please check the installation.",
                    ex
                );
            }
        }

        /// <summary>
        /// Gets the duration of a video file using ffprobe.
        /// </summary>
        /// <param name="inputFilePath">The full path to the video file.</param>
        /// <returns>A <see cref="TimeSpan"/> representing the video's duration, or <see cref="TimeSpan.Zero"/> if it cannot be determined.</returns>
        private async Task<TimeSpan> GetVideoDurationAsync(string inputFilePath)
        {
            var arguments =
                $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFilePath}\"";

            var (output, error) = await RunProcessAsync(_ffprobePath, arguments);

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning(
                    "ffprobe returned an error while getting duration for {InputPath}: {Error}",
                    inputFilePath,
                    error
                );
            }

            if (
                double.TryParse(
                    output,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double seconds
                )
            )
            {
                return TimeSpan.FromSeconds(seconds);
            }

            _logger.LogWarning(
                "Could not parse video duration from ffprobe output for: {InputPath}. Output was: '{Output}'",
                inputFilePath,
                output
            );
            return TimeSpan.Zero;
        }
    }
}
