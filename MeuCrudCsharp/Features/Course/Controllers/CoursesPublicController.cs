using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Courses.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Courses.Controllers
{
    /// <summary>
    /// Endpoints públicos para a visualização de cursos, acessível por usuários e administradores.
    /// </summary>
    [ApiController]
    [Route("api/courses")]
    [AllowAnonymous]
    public class PublicCoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<PublicCoursesController> _logger;

        /// <summary>
        /// Inicializa uma nova instância do controlador público de cursos.
        /// </summary>
        /// <param name="courseService">O serviço para operações de curso.</param>
        /// <param name="logger">O logger para registrar informações e erros.</param>
        public PublicCoursesController(
            ICourseService courseService,
            ILogger<PublicCoursesController> logger
        )
        {
            _courseService = courseService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém uma lista de todos os cursos com seus vídeos associados.
        /// </summary>
        /// <returns>Uma lista de cursos com vídeos.</returns>
        /// <response code="200">Retorna a lista de cursos.</response>
        /// <response code="500">Se ocorrer um erro interno ao buscar os cursos.</response>
        [HttpGet]
        public async Task<IActionResult> GetAllCoursesWithVideos()
        {
            try
            {
                var courses = await _courseService.GetAllCoursesWithVideosAsync();
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cursos com vídeos.");
                return StatusCode(
                    500,
                    new { message = "Não foi possível carregar os cursos no momento." }
                );
            }
        }
    }
}
