using Hangfire;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Videos.Controller
{
    /// <summary>
    /// Manages administrative CRUD operations for videos, including file uploads and metadata management.
    /// Requires 'Admin' role for access.
    /// </summary>
    [ApiController]
    [Route("api/admin/videos")]
    [Authorize(Roles = "Admin")]
    public class AdminVideosController : ControllerBase
    {
        private readonly IAdminVideoService _videoService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AdminVideosController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminVideosController"/> class.
        /// </summary>
        /// <param name="videoService">The service for video metadata operations.</param>
        /// <param name="backgroundJobClient">The client for enqueuing background jobs.</param>
        /// <param name="env">The web hosting environment for file path information.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public AdminVideosController(
            IAdminVideoService videoService,
            IBackgroundJobClient backgroundJobClient,
            IWebHostEnvironment env,
            ILogger<AdminVideosController> logger
        )
        {
            _videoService = videoService;
            _backgroundJobClient = backgroundJobClient;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Receives a video file, saves it temporarily, and enqueues a background job for HLS processing.
        /// </summary>
        /// <param name="videoFile">The video file uploaded via the form.</param>
        /// <returns>An object containing a confirmation message and the unique storage identifier for the video.</returns>
        /// <response code="200">Returns the storage identifier and a confirmation message.</response>
        /// <response code="400">If no file is provided.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs during file handling.</response>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadVideoFile(IFormFile videoFile)
        {
            try
            {
                if (videoFile == null || videoFile.Length == 0)
                {
                    return BadRequest("No file was uploaded.");
                }

                var storageIdentifier = Guid.NewGuid().ToString();
                var videoFolderPath = Path.Combine(_env.WebRootPath, "Videos", storageIdentifier);
                Directory.CreateDirectory(videoFolderPath);

                var inputFilePath = Path.Combine(videoFolderPath, videoFile.FileName);

                await using (var stream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                var outputHlsPath = Path.Combine(videoFolderPath, "hls");

                _backgroundJobClient.Enqueue<VideoProcessingService>(service =>
                    service.ProcessVideoToHlsAsync(inputFilePath, outputHlsPath, storageIdentifier)
                );

                return Ok(
                    new
                    {
                        Message = "Upload received. The video is being processed in the background.",
                        StorageIdentifier = storageIdentifier,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during video upload.");
                return StatusCode(
                    500,
                    "An internal error occurred while handling the file upload."
                );
            }
        }

        /// <summary>
        /// Retrieves a paginated list of all video metadata.
        /// </summary>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A paginated list of videos.</returns>
        /// <response code="200">Returns the paginated list of videos.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<VideoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllVideos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            try
            {
                var videos = await _videoService.GetAllVideosAsync(page, pageSize);
                return Ok(videos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all videos.");
                return StatusCode(500, "An unexpected error occurred while fetching videos.");
            }
        }

        /// <summary>
        /// Creates the metadata for a new video, including its thumbnail.
        /// </summary>
        /// <param name="createDto">A DTO containing the video metadata and the thumbnail file.</param>
        /// <returns>The created video metadata.</returns>
        /// <response code="201">Returns the newly created video.</response>
        /// <response code="400">If the provided data is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPost]
        [ProducesResponseType(typeof(VideoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateVideoMetadata([FromForm] CreateVideoDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var videoToReturn = await _videoService.CreateVideoAsync(createDto);
                return CreatedAtAction(
                    nameof(GetAllVideos),
                    new { id = videoToReturn.Id },
                    videoToReturn
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating video metadata.");
                return StatusCode(500, "An unexpected error occurred while creating the video.");
            }
        }

        /// <summary>
        /// Updates the metadata and optionally the thumbnail of an existing video.
        /// </summary>
        /// <param name="id">The unique identifier of the video to update.</param>
        /// <param name="updateDto">A DTO containing the updated video metadata and an optional new thumbnail file.</param>
        /// <returns>The updated video metadata.</returns>
        /// <response code="200">Returns the updated video.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="404">If a video with the specified ID is not found.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateVideo(Guid id, [FromForm] UpdateVideoDto updateDto)
        {
            try
            {
                var updatedVideo = await _videoService.UpdateVideoAsync(id, updateDto);
                return Ok(updatedVideo);
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating video {VideoId}.", id);
                return StatusCode(500, "An unexpected error occurred while updating the video.");
            }
        }

        /// <summary>
        /// Deletes a video's metadata and its associated files from storage.
        /// </summary>
        /// <param name="id">The unique identifier of the video to delete.</param>
        /// <returns>A confirmation message.</returns>
        /// <response code="200">Indicates the video was successfully deleted.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="404">If a video with the specified ID is not found.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteVideo(Guid id)
        {
            try
            {
                await _videoService.DeleteVideoAsync(id);
                return Ok(
                    new { message = "Video and associated files were deleted successfully." }
                );
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting video {VideoId}.", id);
                return StatusCode(500, "An unexpected error occurred while deleting the video.");
            }
        }
    }
}
