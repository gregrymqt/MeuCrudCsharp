using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Courses.Interfaces
{
    public interface ICourseRepository
    {
        // Consultas
        Task<Course?> GetByPublicIdAsync(Guid publicId);
        Task<Course?> GetByPublicIdWithVideosAsync(Guid publicId); // Necessário para validação de delete
        Task<Course?> GetByNameAsync(string name);
        Task<IEnumerable<Course>> SearchByNameAsync(string name);
        Task<bool> ExistsByNameAsync(string name);

        // Paginação (Retorna os itens e o total para o Service montar o DTO)
        Task<(IEnumerable<Course> Items, int TotalCount)> GetPaginatedWithVideosAsync(
            int pageNumber,
            int pageSize
        );

        // Comandos
        Task AddAsync(Course course);
        void Update(Course course);
        void Delete(Course course);

        // Persistência
        Task SaveChangesAsync();
    }
}
