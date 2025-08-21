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
        /// 
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("paginated")] 
        public async Task<IActionResult> GetCoursesPaginated([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            try
            {
                var paginatedResult = await _courseService.GetCoursesWithVideosPaginatedAsync(pageNumber, pageSize);
                return Ok(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cursos paginados.");
                return StatusCode(500, new { message = "Não foi possível carregar os cursos no momento." });
            }
        }
    }
}
