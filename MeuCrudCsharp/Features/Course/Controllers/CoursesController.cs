using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Courses.Controllers
{
    /// <summary>
    /// Endpoints para o gerenciamento de cursos, acessível apenas por administradores.
    /// </summary>
    [ApiController]
    [Route("api/admin/courses")]
    [Authorize(Roles = "Admin")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        /// <summary>
        /// Inicializa uma nova instância do controlador de cursos.
        /// </summary>
        /// <param name="courseService">O serviço para operações de curso.</param>
        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        /// <summary>
        /// Obtém uma lista de todos os cursos, incluindo seus vídeos associados.
        /// </summary>
        /// <returns>Uma lista de cursos.</returns>
        /// <response code="200">Retorna a lista de cursos.</response>
        /// <response code="500">Se ocorrer um erro interno no servidor.</response>
        [HttpGet]
        public async Task<IActionResult> GetAllCourses()
        {
            try
            {
                var courses = await _courseService.GetAllCoursesWithVideosAsync();
                return Ok(courses);
            }
            catch (AppServiceException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém um curso específico pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do curso a ser recuperado.</param>
        /// <returns>Os dados do curso encontrado.</returns>
        /// <response code="200">Retorna o curso solicitado.</response>
        /// <response code="404">Se o curso com o ID especificado não for encontrado.</response>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetCourseById(Guid id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                return Ok(course);
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cria um novo curso.
        /// </summary>
        /// <param name="createDto">Os dados para a criação do novo curso.</param>
        /// <returns>O curso recém-criado.</returns>
        /// <response code="201">Retorna o curso recém-criado com a localização no cabeçalho.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos (ex: nome duplicado).</response>
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto createDto)
        {
            try
            {
                var newCourse = await _courseService.CreateCourseAsync(createDto);
                return CreatedAtAction(nameof(GetCourseById), new { id = newCourse.Id }, newCourse);
            }
            catch (AppServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza um curso existente.
        /// </summary>
        /// <param name="id">O ID do curso a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o curso.</param>
        /// <returns>O curso atualizado.</returns>
        /// <response code="200">Retorna o curso atualizado.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="404">Se o curso com o ID especificado não for encontrado.</response>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] UpdateCourseDto updateDto)
        {
            try
            {
                var updatedCourse = await _courseService.UpdateCourseAsync(id, updateDto);
                return Ok(updatedCourse);
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (AppServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Exclui um curso.
        /// </summary>
        /// <param name="id">O ID do curso a ser excluído.</param>
        /// <returns>Nenhum conteúdo.</returns>
        /// <response code="204">Se o curso for excluído com sucesso.</response>
        /// <response code="404">Se o curso com o ID especificado não for encontrado.</response>
        /// <response code="409">Se houver um conflito que impede a exclusão (ex: curso com vídeos associados).</response>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            try
            {
                await _courseService.DeleteCourseAsync(id);
                return NoContent();
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (AppServiceException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
