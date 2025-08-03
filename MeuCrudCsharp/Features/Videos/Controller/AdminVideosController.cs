using Hangfire;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Videos.Controller
{
    [ApiController]
    [Route("api/admin/videos")]
    [Authorize(Roles = "Admin")]
    public class AdminVideosController : ControllerBase
    {
        private readonly IAdminVideoService _videoService;

        // Dependências adicionadas para o método de upload
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IWebHostEnvironment _env;

        public AdminVideosController(
            IAdminVideoService videoService,
            IBackgroundJobClient backgroundJobClient,
            IWebHostEnvironment env
        )
        {
            _videoService = videoService;
            _backgroundJobClient = backgroundJobClient;
            _env = env;
        }

        // =======================================================
        // Endpoint de Upload de Arquivo
        // =======================================================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideoFile(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
            {
                return BadRequest("Nenhum arquivo foi enviado.");
            }

            var storageIdentifier = Guid.NewGuid().ToString();
            var videoFolderPath = Path.Combine(_env.WebRootPath, "Videos", storageIdentifier);
            Directory.CreateDirectory(videoFolderPath);

            var inputFilePath = Path.Combine(videoFolderPath, videoFile.FileName);

            using (var stream = new FileStream(inputFilePath, FileMode.Create))
            {
                await videoFile.CopyToAsync(stream);
            }

            var outputHlsPath = Path.Combine(videoFolderPath, "hls");

            // Enfileira o job para processamento em segundo plano.
            _backgroundJobClient.Enqueue<VideoProcessingService>(service =>
                service.ProcessVideoToHlsAsync(inputFilePath, outputHlsPath, storageIdentifier)
            );

            // Retorna o ID para o frontend usar na criação dos metadados.
            return Ok(
                new
                {
                    Message = "Upload recebido. O vídeo está sendo processado em segundo plano.",
                    StorageIdentifier = storageIdentifier,
                }
            );
        }

        // =======================================================
        // Endpoints de CRUD de Metadados
        // =======================================================

        [HttpGet]
        public async Task<IActionResult> GetAllVideos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var videos = await _videoService.GetAllVideosAsync(page, pageSize);
            return Ok(videos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVideoMetadata([FromBody] CreateVideoDto createDto, IFormFile? thumbnailFile)

        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var videoToReturn = await _videoService.CreateVideoAsync(createDto, thumbnailFile);
            return CreatedAtAction(
                nameof(GetAllVideos),
                new { id = videoToReturn.Id },
                videoToReturn
            );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVideo(Guid id, [FromBody] UpdateVideoDto updateDto, IFormFile? thumbnailFile)
        {
            var updatedVideo = await _videoService.UpdateVideoAsync(id, updateDto, thumbnailFile);
            return Ok(updatedVideo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVideo(Guid id)
        {
            await _videoService.DeleteVideoAsync(id);
            
            return Ok(new { message = "Vídeo e arquivos associados foram deletados com sucesso." });
        }
    }
}
