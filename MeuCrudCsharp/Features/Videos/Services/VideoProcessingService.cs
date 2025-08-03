using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Service
{

    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly ILogger<VideoProcessingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string _ffmpegPath = "ffmpeg";
        private const string _ffprobePath = "ffprobe";

        public VideoProcessingService(ILogger<VideoProcessingService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessVideoToHlsAsync(string inputFilePath, string outputDirectory, string storageIdentifier)
        {
            // Usar 'await using' garante que o escopo seja descartado corretamente
           await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            Video? video = null; // Declaramos aqui para usar no bloco catch final

            try
            {
                video = await context.Videos.FirstOrDefaultAsync(v => v.StorageIdentifier == storageIdentifier);
                if (video == null)
                {
                    // MUDANÇA 1: Lança uma exceção clara se o vídeo não for encontrado.
                    // Isso evita que o job seja considerado bem-sucedido.
                    throw new ResourceNotFoundException($"Vídeo com StorageIdentifier {storageIdentifier} não encontrado para processamento.");
                }

                if (!File.Exists(inputFilePath))
                {
                    throw new FileNotFoundException($"Arquivo de entrada não encontrado: {inputFilePath}");
                }

                Directory.CreateDirectory(outputDirectory);

                // --- Passo 1: Obter a duração do vídeo ---
                var duration = await GetVideoDurationAsync(inputFilePath);

                // --- Passo 2: Processar o vídeo com ffmpeg ---
                var manifestPath = Path.Combine(outputDirectory, "manifest.m3u8");
                var arguments = $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{Path.Combine(outputDirectory, "segment%03d.ts")}\" \"{manifestPath}\"";

                await RunProcessAsync(_ffmpegPath, arguments);

                // Se chegou aqui sem exceções, o processo foi bem-sucedido
                video.Status = VideoStatus.Available;
                video.Duration = duration;
                _logger.LogInformation("Vídeo {StorageIdentifier} processado com sucesso.", storageIdentifier);
            }
            // MUDANÇA 2: Bloco catch para lidar com todas as falhas
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica ao processar o vídeo {StorageIdentifier}.", storageIdentifier);

                // Se o vídeo foi encontrado, atualiza seu status para 'Error'
                if (video != null)
                {
                    video.Status = VideoStatus.Error;
                }

                // Relança a exceção para que o Hangfire saiba que o job falhou e aplique as retentativas.
                throw;
            }
            finally
            {
                // MUDANÇA 3: Bloco finally para garantir que o SaveChanges seja chamado
                // tanto em caso de sucesso quanto de erro (para salvar o status 'Error').
                if (video != null && context.ChangeTracker.HasChanges())
                {
                    await context.SaveChangesAsync();
                }
            }
        }


        private async Task RunProcessAsync(string filePath, string arguments)
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

            // MUDANÇA 4: Adicionando try-catch específico para a execução do processo
            try
            {
                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new AppServiceException($"Não foi possível iniciar o processo para '{filePath}'.");
                }

                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    // Lança uma exceção com a saída de erro do FFmpeg
                    throw new AppServiceException($"Processo '{filePath}' falhou com código de saída {process.ExitCode}. Erro: {error}");
                }
            }
            catch (Win32Exception ex) // Erro comum se o ffmpeg não for encontrado
            {
                _logger.LogError(ex, "Erro ao iniciar o processo '{FileName}'. Verifique se o FFmpeg/FFprobe está instalado e no PATH do sistema.", filePath);
                throw new AppServiceException($"Dependência externa '{filePath}' não encontrada. Verifique a instalação.", ex);
            }
        }

        private async Task<TimeSpan> GetVideoDurationAsync(string inputFilePath)
        {
            var arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFilePath}\"";

            // O método RunProcessAsync agora lança exceção em caso de erro, então não precisamos mais de um try-catch aqui
            // A responsabilidade está centralizada
            await RunProcessAsync(_ffprobePath, arguments); // Executa o ffprobe

            // A lógica de ler a saída foi movida para um novo método para maior clareza
            var output = await ReadProcessOutputAsync(_ffprobePath, arguments);

            if (double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds))
            {
                return TimeSpan.FromSeconds(seconds);
            }

            _logger.LogWarning("Não foi possível obter a duração do vídeo: {InputPath}. A saída do ffprobe não foi um número válido.", inputFilePath);
            return TimeSpan.Zero;
        }

        private async Task<string> ReadProcessOutputAsync(string filePath, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) throw new AppServiceException($"Não foi possível iniciar o processo para '{filePath}'.");

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output.Trim();
        }
    }
}
