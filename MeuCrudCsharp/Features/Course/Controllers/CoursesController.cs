using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Course.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Courses.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CoursesController : ApiControllerBase 
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        // ✅ CORRIGIDO: Agora suporta paginação
        [HttpGet("admin")]
        public async Task<IActionResult> GetCoursesPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10
        )
        {
            try
            {
                // Chama o método correto da service, passando os parâmetros da query string
                var paginatedResult = await _courseService.GetCoursesWithVideosPaginatedAsync(
                    pageNumber,
                    pageSize
                );
                return Ok(paginatedResult);
            }
            catch (AppServiceException ex)
            {
                // Este catch pode ser mais genérico, pois a service já trata erros específicos
                return StatusCode(
                    500,
                    new { message = "Ocorreu um erro ao buscar os cursos.", details = ex.Message }
                );
            }
        }

        // ✅ Perfeito, nenhuma alteração necessária
        [HttpGet("/admin/{id:guid}")]
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

        // ✅ Perfeito, nenhuma alteração necessária
        [HttpPost("admin")]
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

        // ✅ Perfeito, nenhuma alteração necessária
        [HttpPut("/admin/{id:guid}")]
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

        // ✅ Perfeito, nenhuma alteração necessária
        [HttpDelete("/admin/{id:guid}")]
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
                // Retornar 'Conflict' aqui é uma excelente escolha de design!
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
