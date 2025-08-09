using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Courses.DTOs;

namespace MeuCrudCsharp.Features.Courses.Interfaces
{
    public interface ICourseService
    {
        Task<List<CourseDto>> GetAllCoursesWithVideosAsync();
        Task<CourseDto?> GetCourseByIdAsync(Guid id);
        Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto);
        Task<CourseDto> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto);
        Task DeleteCourseAsync(Guid id);
    }
}