using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Course.DTOs
{
    public static class CourseMapper
    {
        // Mapeia um único curso com todos os seus vídeos
        public static CourseDto ToDtoWithVideos(Models.Course course)
        {
            return new CourseDto
            {
                Id = course.PublicId,
                Name = course.Name,
                Description = course.Description,
                Videos = course.Videos?.Select(VideoMapper.ToDto).ToList() ?? new List<VideoDto>(),
            };
        }

        // Mapeia um único curso sem a lista de vídeos (para performance)
        public static CourseDto ToDto(Models.Course course)
        {
            return new CourseDto
            {
                Id = course.PublicId,
                Name = course.Name,
                Description = course.Description,
                // A lista de vídeos fica vazia por padrão
            };
        }
    }
}
