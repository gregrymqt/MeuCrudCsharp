using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.Videos.Services;

public class VideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly IVideoRepository _videoRepository; // Novo
    private readonly IFileRepository _fileRepository; // Novo
    private readonly IVideoNotificationService _videoNotificationService;
    private readonly IProcessRunnerService _processRunnerService;
    private readonly FFmpegSettings _ffmpegSettings;
    private readonly IWebHostEnvironment _env;

    // Regex mantido (está correto)
    private static readonly Regex FfmpegProgressRegex = new Regex(
        @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})",
        RegexOptions.Compiled
    );

    public VideoProcessingService(
        ILogger<VideoProcessingService> logger,
        IVideoRepository videoRepository, // Injetando Repository
        IFileRepository fileRepository, // Injetando Repository
        IVideoNotificationService videoNotificationService,
        IProcessRunnerService processRunnerService,
        IOptions<FFmpegSettings> ffmpegSettings,
        IWebHostEnvironment env
    )
    {
        _logger = logger;
        _videoRepository = videoRepository;
        _fileRepository = fileRepository;
        _videoNotificationService = videoNotificationService;
        _processRunnerService = processRunnerService;
        _ffmpegSettings = ffmpegSettings.Value;
        _env = env;
    }

    // Assinatura ajustada para receber IDs (usada pelo AdminVideoService)
    public async Task ProcessVideoToHlsAsync(int videoId, int fileId)
    {
        Video video = null;
        string groupName = "";

        try
        {
            // 1. Buscar Entidades
            video = await _videoRepository.GetByIdAsync(videoId);
            var originalFile = await _fileRepository.GetByIdAsync(fileId);

            if (video == null || originalFile == null)
                throw new ResourceNotFoundException(
                    "Vídeo ou Arquivo original não encontrados no banco."
                );

            groupName = video.StorageIdentifier; // Usamos o ID do storage como grupo do SignalR

            // 2. Definir Caminhos
            // Caminho Entrada: wwwroot/uploads/Videos/guid_nome.mp4
            var inputFilePath = Path.Combine(_env.WebRootPath, originalFile.CaminhoRelativo);

            // Caminho Saída Base: wwwroot/uploads/Videos/{StorageIdentifier}
            var videoDirectory = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "Videos",
                video.StorageIdentifier
            );

            // Subpasta HLS: wwwroot/uploads/Videos/{StorageIdentifier}/hls
            var hlsOutputDirectory = Path.Combine(videoDirectory, "hls");

            // Cria a pasta HLS se não existir
            if (!Directory.Exists(hlsOutputDirectory))
                Directory.CreateDirectory(hlsOutputDirectory);

            // Validação física
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException(
                    $"Arquivo físico não encontrado em: {inputFilePath}"
                );

            // 3. Início do Processo
            await _videoNotificationService.SendProgressUpdate(
                groupName,
                "Iniciando análise...",
                0
            );

            // 4. Obter Duração
            var duration = await GetVideoDurationAsync(inputFilePath);
            video.Duration = duration;
            await _videoRepository.UpdateAsync(video); // Salva duração parcial

            // 5. Configurar FFMPEG para HLS
            var manifestPath = Path.Combine(hlsOutputDirectory, "manifest.m3u8");
            var segmentPattern = Path.Combine(hlsOutputDirectory, "segment%03d.ts");

            // Comando ajustado: Entrada -> HLS (segmentos de 10s) -> Saída na pasta hls/
            var arguments =
                $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{segmentPattern}\" \"{manifestPath}\"";

            // 6. Callback de Progresso
            Func<string, Task> onProgress = rawOutput =>
            {
                var progressPercent = ParseFfmpegProgress(rawOutput, duration.TotalSeconds);
                if (progressPercent.HasValue)
                {
                    return _videoNotificationService.SendProgressUpdate(
                        groupName,
                        "Convertendo...",
                        progressPercent.Value
                    );
                }
                return Task.CompletedTask;
            };

            await _videoNotificationService.SendProgressUpdate(
                groupName,
                "Convertendo vídeo...",
                5
            );

            // Executa FFMPEG
            await _processRunnerService.RunProcessWithProgressAsync(
                _ffmpegSettings.FfmpegPath,
                arguments,
                onProgress
            );

            // 7. Finalização
            await _videoRepository.UpdateStatusAsync(videoId, VideoStatus.Available);

            await _videoNotificationService.SendProgressUpdate(
                groupName,
                "Processamento concluído!",
                100,
                isComplete: true
            );

            _logger.LogInformation(
                "Vídeo {Id} ({StorageIdentifier}) processado com sucesso.",
                videoId,
                video.StorageIdentifier
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no processamento do vídeo ID {VideoId}", videoId);

            if (video != null)
            {
                await _videoRepository.UpdateStatusAsync(videoId, VideoStatus.Error);
                await _videoNotificationService.SendProgressUpdate(
                    groupName,
                    $"Erro: {ex.Message}",
                    0,
                    isError: true
                );
            }
            throw; // Relança para o Hangfire marcar como Failed
        }
    }

    private async Task<TimeSpan> GetVideoDurationAsync(string inputFilePath)
    {
        var arguments =
            $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFilePath}\"";

        var result = await _processRunnerService.RunProcessAndGetOutputAsync(
            _ffmpegSettings.FfprobePath,
            arguments
        );

        if (
            double.TryParse(
                result.StandardOutput,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double seconds
            )
        )
        {
            return TimeSpan.FromSeconds(seconds);
        }

        _logger.LogWarning(
            "Falha ao ler duração. Saída: {Output} Erro: {Error}",
            result.StandardOutput,
            result.StandardError
        );
        return TimeSpan.Zero;
    }

    private int? ParseFfmpegProgress(string ffmpegLine, double totalDurationSeconds)
    {
        if (totalDurationSeconds <= 0)
            return null;

        var match = FfmpegProgressRegex.Match(ffmpegLine);
        if (match.Success)
        {
            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var seconds = int.Parse(match.Groups[3].Value);
            var milliseconds = int.Parse(match.Groups[4].Value);

            var processedTime = new TimeSpan(0, hours, minutes, seconds, milliseconds * 10);
            var progress = (int)(processedTime.TotalSeconds / totalDurationSeconds * 100);

            return Math.Min(99, progress); // Trava em 99% até terminar o processo
        }
        return null;
    }
}
