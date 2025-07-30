using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Course
{
    [ApiController]
    [Route("api/courses")]
    [Authorize] // Garante que apenas usuários logados possam chamar esta API.
    public class CoursesController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public CoursesController(ApiDbContext context)
        {
            _context = context;
        }

        // Endpoint que o JavaScript irá chamar: GET /api/courses/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCoursesWithVideos()
        {
            // Busca todos os cursos, incluindo a lista de vídeos de cada um.
            // Apenas vídeos com status "Available" são incluídos.
            var courses = await _context
                .Courses.Include(course =>
                    course.Videos.Where(video => video.Status == VideoStatus.Available)
                )
                .OrderBy(course => course.Name)
                .Select(course => new // Mapeia para um DTO para moldar a resposta JSON
                {
                    course.Id,
                    course.Name,
                    Videos = course.Videos.Select(video => new
                    {
                        video.Id,
                        video.Title,
                        video.StorageIdentifier,
                        video.Duration,
                    }),
                })
                .ToListAsync();

            return Ok(courses);
        }
    }
}
