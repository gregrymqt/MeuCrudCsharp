using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Courses.DTOs;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Courses.Controllers
{
    [ApiController]
    [Route("api/admin/courses")]
    [Authorize(Roles = "Admin")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

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
                // Pode ser um erro de "nome duplicado" ou um erro inesperado
                return BadRequest(new { message = ex.Message });
            }
        }

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

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            try
            {
                await _courseService.DeleteCourseAsync(id);
                return NoContent(); // 204 No Content é a resposta padrão para um DELETE bem-sucedido
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (AppServiceException ex) // Captura a regra de negócio (curso com vídeos)
            {
                return Conflict(new { message = ex.Message }); // 409 Conflict é um bom status para falha de regra de negócio
            }
        }
    }
}