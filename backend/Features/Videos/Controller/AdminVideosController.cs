using Hangfire;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Videos.Controller
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/videos")]
    public class AdminVideosController : ApiControllerBase
    {
        private readonly IAdminVideoService _videoService;
        
        // Removemos IWebHostEnvironment e IBackgroundJobClient daqui.
        // Quem usa eles agora é o Service (internamente).
        public AdminVideosController(IAdminVideoService videoService)
        {
            _videoService = videoService;
        }

        /// <summary>
        /// Endpoint unificado: Recebe o arquivo e os dados, salva tudo e dispara o processamento.
        /// </summary>
        [HttpPost] // Agora é um POST na raiz /api/admin/videos
        public async Task<IActionResult> CreateVideo(
            [FromForm] IFormFile videoFile, 
            [FromForm] string title, 
            [FromForm] string description,
            [FromForm] IFormFile? thumbnailFile)
        {
            if (videoFile == null || videoFile.Length == 0)
                return BadRequest("O arquivo de vídeo é obrigatório.");

            if (string.IsNullOrEmpty(title))
                return BadRequest("O título do vídeo é obrigatório.");

            // O Service cuida de salvar o arquivo (UploadService) e salvar no Banco (Repository)
            var videoCriado = await _videoService.HandleVideoUploadAsync(videoFile, title, description, thumbnailFile);

            return CreatedAtAction(nameof(GetAllVideos), new { id = videoCriado.Id }, videoCriado);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVideos([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _videoService.GetAllVideosAsync(page, pageSize);
            return Ok(result);
        }

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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteVideo(Guid id)
        {
            try
            {
                await _videoService.DeleteVideoAsync(id);
                return Ok(new { Message = "Vídeo e arquivos deletados com sucesso." });
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
        }
    }
}
