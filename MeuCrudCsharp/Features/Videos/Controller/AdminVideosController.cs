using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Videos.Controller
{
    [ApiController]
    [Route("api/admin/videos")]
    [Authorize(Roles = "Admin")]
    public class AdminVideosController : ControllerBase
    {
        private readonly IAdminVideoService _videoService;
        private readonly IBackgroundJobClient _jobs;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AdminVideosController> _logger;

        public AdminVideosController(
            IAdminVideoService videoService,
            IBackgroundJobClient jobs,
            IWebHostEnvironment env,
            ILogger<AdminVideosController> logger
        )
        {
            _videoService = videoService;
            _jobs = jobs;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Recebe o arquivo de v�deo, salva em disco e dispara o processamento em background.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadVideoFile(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
                return BadRequest("Nenhum arquivo foi enviado.");

            var storageId = Guid.NewGuid().ToString();
            var folder = Path.Combine(_env.WebRootPath, "videos", storageId);
            Directory.CreateDirectory(folder);

            var inputPath = Path.Combine(folder, videoFile.FileName);
            await using var fs = new FileStream(inputPath, FileMode.Create);
            await videoFile.CopyToAsync(fs);

            var hlsPath = Path.Combine(folder, "hls");
            _jobs.Enqueue<IVideoProcessingService>(svc =>
                svc.ProcessVideoToHlsAsync(inputPath, hlsPath, storageId)
            );

            return Ok(
                new
                {
                    Message = "Upload recebido. V�deo ser� processado em segundo plano.",
                    StorageIdentifier = storageId,
                }
            );
        }

        // <summary>
        /// Lista v�deos paginados.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllVideos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _videoService.GetAllVideosAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Cria apenas a metadata do v�deo (depois do upload e processamento).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateVideoMetadata([FromForm] CreateVideoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _videoService.CreateVideoAsync(dto);
            return CreatedAtAction(nameof(GetAllVideos), new { id = created.Id }, created);
        }

        /// <summary>
        /// Atualiza t�tulo, descri��o e thumbnail de um v�deo j� existente.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateVideo(Guid id, [FromForm] UpdateVideoDto dto)
        {
            try
            {
                var updated = await _videoService.UpdateVideoAsync(id, dto);
                return Ok(updated);
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
        }

        /// <summary>
        /// Remove um v�deo, seus ativos (HLS, arquivos) e invalida cache.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteVideo(Guid id)
        {
            try
            {
                await _videoService.DeleteVideoAsync(id);
                return Ok(new { Message = "V�deo deletado com sucesso." });
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
        }
    }
}
