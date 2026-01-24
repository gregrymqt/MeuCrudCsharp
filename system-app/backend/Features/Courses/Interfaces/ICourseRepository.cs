using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Courses.Interfaces
{
    public interface ICourseRepository
    {
        // Consultas
        Task<Course?> GetByPublicIdAsync(Guid publicId);
        Task<Course?> GetByPublicIdWithVideosAsync(Guid publicId);
        Task<Course?> GetByNameAsync(string name);
        Task<IEnumerable<Course>> SearchByNameAsync(string name);
        Task<bool> ExistsByNameAsync(string name);

        // Paginação
        Task<(IEnumerable<Course> Items, int TotalCount)> GetPaginatedWithVideosAsync(
            int pageNumber,
            int pageSize
        );

        // Comandos (Só rastreiam mudanças, não persistem)
        Task AddAsync(Course course);
        void Update(Course course);
        void Delete(Course course);

        // SaveChangesAsync removido - UnitOfWork é responsável
    }
}
