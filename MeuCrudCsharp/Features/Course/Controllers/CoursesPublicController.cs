using System.Threading.Tasks;
using MeuCrudCsharp.Features.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Courses.Controllers
{
    [ApiController]
    [Route("api/courses")] // Rota base: /api/courses
    public class PublicCoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public PublicCoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [Authorize(Roles = "Admin,User")] // Permite acesso apenas para usuários autenticados com as roles Admin ou User
        [HttpGet("all")] // Rota final: GET /api/courses/all
        public async Task<IActionResult> GetAllCoursesWithVideos()
        {
            try
            {
                var courses = await _courseService.GetAllCoursesWithVideosAsync();
                return Ok(courses);
            }
            catch (System.Exception ex)
            {
                // Em um app real, logar o erro `ex`
                return StatusCode(500, "Não foi possível carregar os cursos no momento.");
            }
        }
    }
}
