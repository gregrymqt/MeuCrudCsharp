using Hangfire;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Attributes;
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

        [HttpPost]
        [AllowLargeFile(3072)] // 3GB
        public async Task<IActionResult> CreateVideo([FromForm] CreateVideoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Retorna NULL se for chunk intermediário
                var result = await _videoService.HandleVideoUploadAsync(dto);

                if (result == null)
                {
                    return Ok(new { message = $"Chunk {dto.ChunkIndex} do vídeo recebido." });
                }

                // Tudo concluído
                return CreatedAtAction(nameof(GetAllVideos), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVideos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _videoService.GetAllVideosAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        [AllowLargeFile(1024)] // Permite uploads de até 1GB para atualizações
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
