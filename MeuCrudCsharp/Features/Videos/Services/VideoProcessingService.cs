using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace MeuCrudCsharp.Features.Videos.Services
{
    /// <summary>
    /// Implements <see cref="IVideoProcessingService"/> to handle video file conversions using FFmpeg.
    /// This service is designed to be run in the background (e.g., by Hangfire).
    /// </summary>
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ILogger<VideoProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IVideoNotificationService _videoNotificationService;
        private readonly IProcessRunnerService _processRunnerService;
        private readonly FFmpegSettings _ffmpegSettings;
        private readonly IWebHostEnvironment _env;

        // Regex para capturar a informação de tempo (time=HH:mm:ss.ms) da saída do FFmpeg
        private static readonly Regex FfmpegProgressRegex = new Regex(
            @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})",
            RegexOptions.Compiled
        );

        public VideoProcessingService(
            ILogger<VideoProcessingService> logger,
            IServiceProvider serviceProvider,
            IVideoNotificationService videoNotificationService,
            IProcessRunnerService processRunnerService,
            IOptions<FFmpegSettings> ffmpegSettings,
            IWebHostEnvironment env) // <-- INJETAR AQUI
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _videoNotificationService = videoNotificationService;
            _processRunnerService = processRunnerService;
            _ffmpegSettings = ffmpegSettings.Value;
            _env = env; // <-- ATRIBUIR AQUI
        }

        public async Task ProcessVideoToHlsAsync(string storageIdentifier, string originalFileName)
        {
            // 1. MONTAR OS CAMINHOS DE ARQUIVO AQUI
            var basePath = Path.Combine(_env.WebRootPath, "uploads"); // Define o diretório base de uploads
            var outputDirectory = Path.Combine(basePath, storageIdentifier);
            var inputFilePath = Path.Combine(outputDirectory, originalFileName);
            
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            Video? video = await context.Videos.FirstOrDefaultAsync(v => v.StorageIdentifier == storageIdentifier);

            if (video == null)
                throw new ResourceNotFoundException($"Vídeo {storageIdentifier} não encontrado.");

            var groupName = $"processing-{storageIdentifier}";

            try
            {
                await _videoNotificationService.SendProgressUpdate(groupName, "Iniciando...", 0);

                if (!File.Exists(inputFilePath))
                    throw new FileNotFoundException($"Arquivo de entrada não encontrado: {inputFilePath}");
                
                await _videoNotificationService.SendProgressUpdate(groupName, "Obtendo duração do vídeo...", 5);
                var duration = await GetVideoDurationAsync(inputFilePath);
                video.Duration = duration;

                var manifestPath = Path.Combine(outputDirectory, "manifest.m3u8");
                var arguments =
                    $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{Path.Combine(outputDirectory, "segment%03d.ts")}\" \"{manifestPath}\"";

                Func<string, Task> onProgress = rawFfmpegOutput =>
                {
                    var progress = ParseFfmpegProgress(rawFfmpegOutput, duration.TotalSeconds);
                    if (progress.HasValue)
                    {
                        return _videoNotificationService.SendProgressUpdate(groupName, "Convertendo...",
                            progress.Value);
                    }

                    return Task.CompletedTask;
                };

                await _processRunnerService.RunProcessWithProgressAsync(_ffmpegSettings.FfmpegPath, arguments, onProgress);

                video.Status = VideoStatus.Available;
                await _videoNotificationService.SendProgressUpdate(
                    groupName,
                    "Processamento concluído!",
                    100,
                    isComplete: true
                );
                _logger.LogInformation("Vídeo {StorageIdentifier} processado com sucesso.", storageIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica ao processar o vídeo {StorageIdentifier}.", storageIdentifier);
                if (video != null) video.Status = VideoStatus.Error;
                await _videoNotificationService.SendProgressUpdate(groupName, $"Erro: {ex.Message}", 100,
                    isError: true);
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

        // ✅ 4. Novo método para "traduzir" a saída do FFmpeg em porcentagem
        private int? ParseFfmpegProgress(string ffmpegLine, double totalDurationSeconds)
        {
            if (totalDurationSeconds <= 0)
                return null;

            var match = FfmpegProgressRegex.Match(ffmpegLine);
            if (match.Success)
            {
                // Extrai horas, minutos, segundos e milissegundos
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value);

                var processedTime = new TimeSpan(0, hours, minutes, seconds, milliseconds * 10);

                // Calcula a porcentagem, garantindo que não passe de 99% (100% será enviado no final)
                var progress = (int)((processedTime.TotalSeconds / totalDurationSeconds) * 100);
                return Math.Min(99, progress);
            }

            return null;
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

            // A chamada agora é explícita e sem ambiguidades, corrigindo o erro
            var result =
                await _processRunnerService.RunProcessAndGetOutputAsync(_ffmpegSettings.FfprobePath, arguments);
            var output = result.StandardOutput;
            var error = result.StandardError;

            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("ffprobe retornou um erro ao obter a duração: {Error}", error);
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
                "Não foi possível extrair a duração da saída do ffprobe: '{Output}'",
                output
            );
            return TimeSpan.Zero;
        }
    }
}