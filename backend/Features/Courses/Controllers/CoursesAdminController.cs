using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
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

        [HttpGet]
        public async Task<IActionResult> GetCoursesPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10
        )
        {
            try
            {
                var paginatedResult = await _courseService.GetCoursesWithVideosPaginatedAsync(
                    pageNumber,
                    pageSize
                );
                return Ok(paginatedResult);
            }
            catch (AppServiceException ex)
            {
                return StatusCode(
                    500,
                    new { message = "Erro ao buscar cursos.", details = ex.Message }
                );
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCoursesByPublicId(Guid id)
        {
            // CORREÇÃO: Guid nunca é null. Verifica se é vazio (0000-000...)
            if (id == Guid.Empty)
            {
                return BadRequest(new { message = "O ID não pode ser vazio." });
            }

            // A Service retorna a entidade 'Course' (conforme sua interface)
            var course = await _courseService.FindCourseByPublicIdOrFailAsync(id);

            // Mapeamento manual para DTO (Correto)
            var courseDto = new CourseDto
            {
                PublicId = course.PublicId,
                Name = course.Name,
                Description = course.Description,
                // Mapeie os vídeos se necessário
                Videos =
                    course
                        .Videos?.Select(v => new VideoDto
                        {
                            Id = v.PublicId, // Assumindo que VideoDto tem Id/PublicId
                            Title = v.Title,
                        })
                        .ToList() ?? new List<VideoDto>(),
            };

            return Ok(courseDto);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> SearchCoursesByNameAsync(
            [FromQuery] string name
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(Enumerable.Empty<CourseDto>());
            }

            var courses = await _courseService.SearchCoursesByNameAsync(name);
            return Ok(courses);
        }

        [HttpPost]
        // CORREÇÃO: Usando o nome correto do DTO definido na interface
        public async Task<IActionResult> CreateCourse([FromBody] CreateUpdateCourseDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var newCourse = await _courseService.CreateCourseAsync(createDto);
                return CreatedAtAction(
                    nameof(GetCoursesByPublicId), // Melhor apontar para o GetById
                    new { id = newCourse.PublicId },
                    newCourse
                );
            }
            catch (AppServiceException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        // CORREÇÃO: Usando o nome correto do DTO definido na interface
        public async Task<IActionResult> UpdateCourse(
            Guid id,
            [FromBody] CreateUpdateCourseDto updateDto
        )
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
