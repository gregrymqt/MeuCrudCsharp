using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Videos.Controller
{
    [ApiController]
    [Authorize]
    [Route("api/videos")]
    public class VideosController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VideosController(ApiDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Rota para o manifesto: ex: /api/videos/GUID-DO-VIDEO/manifest.m3u8
        [HttpGet("{storageIdentifier}/manifest.m3u8")]
        // [Authorize] // Garante que apenas usuários logados possam assistir
        public async Task<IActionResult> GetManifest(string storageIdentifier)
        {
            // PASSO 1: O usuário clicou, pegamos o ID (storageIdentifier).
            // PASSO 2: Buscamos no banco de dados para validar.
            var videoExists = await _context.Videos.AnyAsync(v =>
                v.StorageIdentifier == storageIdentifier && v.Status == VideoStatus.Available
            );

            if (!videoExists)
            {
                return NotFound("Vídeo não encontrado ou ainda não está disponível.");
            }

            // PASSO 3: Se o vídeo existe, montamos o caminho para o arquivo JÁ PROCESSADO.
            var manifestPath = Path.Combine(
                _env.WebRootPath,
                "Videos",
                storageIdentifier,
                "hls",
                "manifest.m3u8"
            );

            if (!System.IO.File.Exists(manifestPath))
            {
                // Isso indicaria um erro no processamento do FFmpeg.
                return StatusCode(
                    500,
                    "Arquivo de manifesto não encontrado no servidor, apesar de constar no banco de dados."
                );
            }

            // PASSO 4: Entregamos o arquivo de manifesto para o player.
            return PhysicalFile(manifestPath, "application/vnd.apple.mpegurl");
        }

        // Rota para os segmentos: ex: /api/videos/GUID-DO-VIDEO/hls/segment001.ts
        [HttpGet("{storageIdentifier}/hls/{segmentName}")]
        // [Authorize]
        public IActionResult GetVideoSegment(string storageIdentifier, string segmentName)
        {
            // Aqui não precisamos verificar o banco de dados para cada chunk,
            // pois a verificação principal já foi feita no manifesto.
            var segmentPath = Path.Combine(
                _env.WebRootPath,
                "Videos",
                storageIdentifier,
                "hls",
                segmentName
            );

            if (!System.IO.File.Exists(segmentPath))
            {
                return NotFound("Segmento de vídeo não encontrado.");
            }

            return PhysicalFile(segmentPath, "video/mp2t");
        }
    }
}
