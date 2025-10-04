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
        private readonly IVideoNotificationService _videoNotificationService; // ⭐️ 1. Injetar o HubContext
        private const string _ffmpegPath = "ffmpeg";
        private const string _ffprobePath = "ffprobe";

        // Regex para capturar a informação de tempo (time=HH:mm:ss.ms) da saída do FFmpeg
        private static readonly Regex FfmpegProgressRegex = new Regex(
            @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})",
            RegexOptions.Compiled
        );

        public VideoProcessingService(
            ILogger<VideoProcessingService> logger,
            IServiceProvider serviceProvider,
            IVideoNotificationService videoNotificationService) // ⭐️ 1. Receber no construtor
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _videoNotificationService = videoNotificationService;
        }

        // ✅ 2. O método principal agora orquestra as notificações do SignalR
        public async Task ProcessVideoToHlsAsync(
            string inputFilePath,
            string outputDirectory,
            string storageIdentifier
        )
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            Video? video = await context.Videos.FirstOrDefaultAsync(v =>
                v.StorageIdentifier == storageIdentifier
            );

            if (video == null)
                throw new ResourceNotFoundException($"Vídeo {storageIdentifier} não encontrado.");

            var groupName = $"processing-{storageIdentifier}";

            try
            {
                await _videoNotificationService.SendProgressUpdate(groupName, "Iniciando...", 0);

                if (!File.Exists(inputFilePath))
                    throw new FileNotFoundException(
                        $"Arquivo de entrada não encontrado: {inputFilePath}"
                    );

                Directory.CreateDirectory(outputDirectory);

                await _videoNotificationService.SendProgressUpdate(groupName, "Obtendo duração do vídeo...", 5);
                var duration = await GetVideoDurationAsync(inputFilePath);
                video.Duration = duration; // Salva a duração no início

                var manifestPath = Path.Combine(outputDirectory, "manifest.m3u8");
                var arguments =
                    $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{Path.Combine(outputDirectory, "segment%03d.ts")}\" \"{manifestPath}\"";

                // Define a função de callback que será executada para cada linha de progresso do FFmpeg
                Func<string, Task> onProgress = rawFfmpegOutput =>
                {
                    var progress = ParseFfmpegProgress(rawFfmpegOutput, duration.TotalSeconds);
                    if (progress.HasValue)
                    {
                        // Envia a porcentagem calculada via SignalR
                        return _videoNotificationService.SendProgressUpdate(groupName, "Convertendo...", progress.Value);
                    }
                    return Task.CompletedTask;
                };

                // Chama o processo FFmpeg passando o callback de progresso
                await RunProcessWithProgressAsync(_ffmpegPath, arguments, onProgress);

                video.Status = VideoStatus.Available;
                await _videoNotificationService.SendProgressUpdate(
                    groupName,
                    "Processamento concluído!",
                    100,
                    isComplete: true
                );
                _logger.LogInformation(
                    "Vídeo {StorageIdentifier} processado com sucesso.",
                    storageIdentifier
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha crítica ao processar o vídeo {StorageIdentifier}.",
                    storageIdentifier
                );
                if (video != null)
                    video.Status = VideoStatus.Error;

                await _videoNotificationService.SendProgressUpdate(groupName, $"Erro: {ex.Message}", 100, isError: true);
                throw; // Relança a exceção para que o Hangfire/job runner saiba que falhou
            }
            finally
            {
                if (video != null && context.ChangeTracker.HasChanges())
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        private async Task RunProcessWithProgressAsync(
            string filePath,
            string arguments,
            Func<string, Task> onProgress
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

            using var process = Process.Start(processStartInfo);
            if (process == null)
                throw new AppServiceException($"Não foi possível iniciar o processo '{filePath}'.");

            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (line != null)
                {
                    await onProgress(line);
                }
            }

            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new AppServiceException(
                    $"Processo '{filePath}' falhou com código {process.ExitCode}. Erro: {error}"
                );
            }
        }

        // ✅ CORRIGIDO: Método renomeado para maior clareza
        private async Task<(
            string StandardOutput,
            string StandardError
        )> RunProcessAndGetOutputAsync(string filePath, string arguments)
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

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new AppServiceException($"Não foi possível iniciar o processo '{filePath}'.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            // Verifica o código de saída ANTES de retornar, para garantir que o processo foi bem-sucedido
            if (process.ExitCode != 0)
            {
                string error = await errorTask;
                throw new AppServiceException(
                    $"Processo '{filePath}' falhou com código {process.ExitCode}. Erro: {error}"
                );
            }

            return (await outputTask, await errorTask);
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
            var (output, error) = await RunProcessAndGetOutputAsync(_ffprobePath, arguments);

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
