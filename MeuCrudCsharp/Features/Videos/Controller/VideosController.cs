using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Videos.Controller
{
    /// <summary>
    /// Serves HLS (HTTP Live Streaming) video content to authenticated users.
    /// This controller handles requests for both the main manifest file (.m3u8) and the individual video segments (.ts).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/videos")]
    public class VideosController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<VideosController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideosController"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="env">The web hosting environment for file path information.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public VideosController(
            ApiDbContext context,
            IWebHostEnvironment env,
            ILogger<VideosController> logger
        )
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the HLS manifest file (.m3u8) for a specific video.
        /// </summary>
        /// <remarks>
        /// This endpoint is the entry point for video playback. It validates that the requested video
        /// exists and is available before serving the manifest file to the client's player.
        /// </remarks>
        /// <param name="storageIdentifier">The unique identifier of the video.</param>
        /// <returns>The manifest file if the video is found and available.</returns>
        /// <response code="200">Returns the manifest file with the correct content type.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the video is not found or is not yet available.</response>
        /// <response code="500">If the manifest file is missing on the server, indicating a processing error.</response>
        [HttpGet("{storageIdentifier}/manifest.m3u8")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetManifest(string storageIdentifier)
        {
            var videoExists = await _context.Videos.AnyAsync(v =>
                v.StorageIdentifier == storageIdentifier && v.Status == VideoStatus.Available
            );

            if (!videoExists)
            {
                return NotFound("Video not found or is not yet available.");
            }

            var manifestPath = Path.Combine(
                _env.WebRootPath,
                "Videos",
                storageIdentifier,
                "hls",
                "manifest.m3u8"
            );

            if (!System.IO.File.Exists(manifestPath))
            {
                _logger.LogError(
                    "Manifest file not found for video {StorageIdentifier}, but it exists in the database. This indicates a processing error.",
                    storageIdentifier
                );
                return StatusCode(
                    500,
                    "Manifest file not found on the server, indicating a processing error."
                );
            }

            return PhysicalFile(manifestPath, "application/vnd.apple.mpegurl");
        }

        /// <summary>
        /// Retrieves an HLS video segment file (.ts).
        /// </summary>
        /// <remarks>
        /// This endpoint serves the individual video chunks requested by the player. For performance,
        /// it does not re-validate against the database for every segment request, assuming the
        /// initial manifest request was already authorized and validated.
        /// </remarks>
        /// <param name="storageIdentifier">The unique identifier of the video.</param>
        /// <param name="segmentName">The name of the video segment file (e.g., "segment001.ts").</param>
        /// <returns>The video segment file if it exists.</returns>
        /// <response code="200">Returns the video segment file with the correct content type.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="404">If the requested segment file does not exist.</response>
        [HttpGet("{storageIdentifier}/hls/{segmentName}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetVideoSegment(string storageIdentifier, string segmentName)
        {
            var segmentPath = Path.Combine(
                _env.WebRootPath,
                "Videos",
                storageIdentifier,
                "hls",
                segmentName
            );

            if (!System.IO.File.Exists(segmentPath))
            {
                return NotFound("Video segment not found.");
            }

            return PhysicalFile(segmentPath, "video/mp2t");
        }
    }
}
