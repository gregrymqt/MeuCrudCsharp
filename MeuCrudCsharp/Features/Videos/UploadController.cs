using Hangfire;
using MeuCrudCsharp.Features.Videos.Service;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IWebHostEnvironment _env;

    // Injetamos a interface do Hangfire para enfileirar jobs
    // e o IWebHostEnvironment para nos ajudar com os caminhos das pastas
    public UploadController(IBackgroundJobClient backgroundJobClient, IWebHostEnvironment env)
    {
        _backgroundJobClient = backgroundJobClient;
        _env = env;
    }

    [HttpPost("video")]
    // O IFormFile representa o arquivo enviado pelo usuário
    public async Task<IActionResult> UploadVideo(IFormFile videoFile)
    {
        if (videoFile == null || videoFile.Length == 0)
        {
            return BadRequest("Nenhum arquivo foi enviado.");
        }

        // --- 1. Salvar o arquivo original temporariamente ---

        // Criar um nome de pasta único para evitar colisões de arquivos
        var uniqueFolderName = Guid.NewGuid().ToString();
        var videoCoursePath = Path.Combine(_env.WebRootPath, "Videos", uniqueFolderName);
        Directory.CreateDirectory(videoCoursePath);

        // O caminho completo do arquivo de entrada que o FFmpeg irá ler
        var inputFilePath = Path.Combine(videoCoursePath, videoFile.FileName);

        // Salva o arquivo enviado no disco
        using (var stream = new FileStream(inputFilePath, FileMode.Create))
        {
            await videoFile.CopyToAsync(stream);
        }

        // --- 2. Enfileirar o Job no Hangfire ---

        // O diretório onde os chunks (.ts) e o manifesto (.m3u8) serão salvos
        var outputHlsPath = Path.Combine(videoCoursePath, "hls");

        // Esta é a mágica!
        // Pedimos ao Hangfire para executar o método ProcessVideoToHlsAsync
        // da classe VideoProcessingService, passando os caminhos como argumentos.
        _backgroundJobClient.Enqueue<VideoProcessingService>(
            service => service.ProcessVideoToHlsAsync(inputFilePath, outputHlsPath)
        );

        // --- 3. Retornar uma resposta imediata para o usuário ---

        // Retornamos um ID único que pode ser usado para verificar o status do vídeo depois
        return Ok(new
        {
            Message = "Upload recebido com sucesso! O vídeo está sendo processado.",
            VideoId = uniqueFolderName
        });
    }
}