using System.Diagnostics;
using System.Globalization;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Service
{
    // É uma boa prática criar uma interface para seus serviços
    public interface IVideoProcessingService
    {
        Task ProcessVideoToHlsAsync(
            string inputFilePath,
            string outputDirectory,
            string storageIdentifier
        );
    }

    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ILogger<VideoProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;

        // Usamos IServiceProvider para criar um escopo de banco de dados separado,
        // o que é uma prática recomendada para serviços de longa duração como os do Hangfire.
        public VideoProcessingService(
            ILogger<VideoProcessingService> logger,
            IServiceProvider serviceProvider
        )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _ffmpegPath = "ffmpeg"; // Assume que está no PATH do sistema
            _ffprobePath = "ffprobe"; // Assume que está no PATH do sistema
        }

        public async Task ProcessVideoToHlsAsync(
            string inputFilePath,
            string outputDirectory,
            string storageIdentifier
        )
        {
            // Cria um escopo para usar o DbContext de forma segura em uma thread de background
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

                // Encontra o registro do vídeo que precisa ser atualizado
                var video = await context.Videos.FirstOrDefaultAsync(v =>
                    v.StorageIdentifier == storageIdentifier
                );
                if (video == null)
                {
                    _logger.LogError(
                        "Vídeo com StorageIdentifier {id} não encontrado no banco de dados para processamento.",
                        storageIdentifier
                    );
                    return;
                }

                if (!File.Exists(inputFilePath))
                {
                    _logger.LogError("Arquivo de entrada não encontrado: {path}", inputFilePath);
                    video.Status = VideoStatus.Error;
                    await context.SaveChangesAsync();
                    return;
                }

                Directory.CreateDirectory(outputDirectory);

                // --- Passo 1: Obter a duração do vídeo com ffprobe ---
                var duration = await GetVideoDurationAsync(inputFilePath);

                // --- Passo 2: Processar o vídeo com ffmpeg ---
                var manifestPath = Path.Combine(outputDirectory, "manifest.m3u8");
                var arguments =
                    $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{Path.Combine(outputDirectory, "segment%03d.ts")}\" \"{manifestPath}\"";

                var success = await RunProcessAsync(_ffmpegPath, arguments);

                if (success)
                {
                    _logger.LogInformation("Vídeo {id} processado com sucesso.", storageIdentifier);
                    video.Status = VideoStatus.Available;
                    video.Duration = duration; // Salva a duração obtida
                }
                else
                {
                    _logger.LogError("Falha ao processar o vídeo {id}.", storageIdentifier);
                    video.Status = VideoStatus.Error;
                }

                // Salva as alterações (status e duração) no banco de dados
                await context.SaveChangesAsync();
            }
        }

        private async Task<bool> RunProcessAsync(string filePath, string arguments)
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

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                    return true;

                _logger.LogError(
                    "Erro na execução do processo {FileName}. Erro: {Error}",
                    filePath,
                    error
                );
                return false;
            }
        }

        private async Task<TimeSpan> GetVideoDurationAsync(string inputFilePath)
        {
            var arguments =
                $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFilePath}\"";
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ffprobePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

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
                return TimeSpan.Zero;
            }
        }
    }
}
