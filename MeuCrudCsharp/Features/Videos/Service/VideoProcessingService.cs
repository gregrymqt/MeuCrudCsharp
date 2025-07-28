using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Service;
public class VideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly string _ffmpegPath;

    // Usamos ILogger para registrar o que acontece, essencial para depurar!
    // Você pode pegar o caminho do ffmpeg do appsettings.json para ser mais flexível.
    public VideoProcessingService(ILogger<VideoProcessingService> logger)
    {
        _logger = logger;
        // Assume que o ffmpeg está no PATH do sistema.
        // Em produção, seria melhor configurar o caminho exato no appsettings.json.
        _ffmpegPath = "ffmpeg";
    }

    public async Task<bool> ProcessVideoToHlsAsync(string inputFilePath, string outputDirectory)
    {
        if (!File.Exists(inputFilePath))
        {
            _logger.LogError("Arquivo de entrada não encontrado: {path}", inputFilePath);
            return false;
        }

        Directory.CreateDirectory(outputDirectory); // Garante que o diretório de saída exista

        var manifestName = "manifest.m3u8";
        var manifestPath = Path.Combine(outputDirectory, manifestName);
        var segmentNameTemplate = "segment%03d.ts";

        // Monta a string de argumentos para o FFmpeg
        var arguments = $"-i \"{inputFilePath}\" -c:v libx264 -c:a aac -hls_time 10 -hls_playlist_type vod -hls_segment_filename \"{Path.Combine(outputDirectory, segmentNameTemplate)}\" \"{manifestPath}\"";

        _logger.LogInformation("Iniciando FFmpeg com os argumentos: {args}", arguments);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true, // Captura a saída padrão
            RedirectStandardError = true,  // Captura a saída de erro
            UseShellExecute = false,
            CreateNoWindow = true,         // Não cria uma janela de console visível
        };

        using (var process = new Process { StartInfo = processStartInfo })
        {
            process.Start();

            // Lê as saídas de forma assíncrona para não bloquear
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(); // Espera o processo terminar

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("FFmpeg processado com sucesso. Saída: {output}", output);
                return true;
            }
            else
            {
                _logger.LogError("Erro ao processar com FFmpeg. Código de Saída: {exitCode}. Erro: {error}", process.ExitCode, error);
                return false;
            }
        }
    }
}