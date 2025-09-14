using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Courses.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/courses")]
    public class CoursesAdminController : ApiControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesAdminController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        // ✅ CORRIGIDO: Agora suporta paginação
        [HttpGet]
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCoursesByPublicId(Guid id)
        {
            if (String.IsNullOrEmpty(id.ToString()))
            {
                throw new Exception("O ID não pode ser vazio.");
            }
            
            var course =  await _courseService.FindCourseByPublicIdOrFailAsync(id);

            var courseDto = new CourseDto
            {
                PublicId = course.PublicId,
                Name = course.Name,
                Description = course.Description
            };

            return Ok(courseDto);

        }

        // ✅ Perfeito, nenhuma alteração necessária
        // Na sua classe de controle (ex: AdminCoursesController.cs)

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> SearchCoursesByNameAsync([FromQuery] string name)
        {
            // A validação inicial continua sendo uma boa prática
            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(Enumerable.Empty<CourseDto>());
            }

            // ✅ Chama o novo método da service que retorna uma lista
            var courses = await _courseService.SearchCoursesByNameAsync(name);

            // ✅ Retorna a lista obtida. O try-catch não é mais necessário aqui
            //    porque a service não lança mais a exceção para busca vazia.
            return Ok(courses);
        }

        // ✅ Perfeito, nenhuma alteração necessária
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto createDto)
        {
            try
            {
                var newCourse = await _courseService.CreateCourseAsync(createDto);
                return CreatedAtAction(nameof(SearchCoursesByNameAsync), new { name = newCourse.Name }, newCourse);
            }
            catch (AppServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ Perfeito, nenhuma alteração necessária
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

        // ✅ Perfeito, nenhuma alteração necessária
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
                // Retornar 'Conflict' aqui é uma excelente escolha de design!
                return Conflict(new { message = ex.Message });
            }
        }
    }
}