using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Courses.Repositories
{
    public class CourseRepository(ApiDbContext context) : ICourseRepository
    {
        public async Task<Course?> GetByPublicIdAsync(Guid publicId)
        {
            return await context.Courses.FirstOrDefaultAsync(c => c.PublicId == publicId);
        }

        public async Task<Course?> GetByPublicIdWithVideosAsync(Guid publicId)
        {
            return await context
                .Courses.Include(c => c.Videos) // Carrega os vídeos (usado no DeleteCourseAsync)
                .FirstOrDefaultAsync(c => c.PublicId == publicId);
        }

        public async Task<Course?> GetByNameAsync(string name)
        {
            return await context.Courses.FirstOrDefaultAsync(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        }

        public async Task<IEnumerable<Course>> SearchByNameAsync(string name)
        {
            return await context
                .Courses.AsNoTracking()
                .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await context.Courses.AnyAsync(c => 
                EF.Functions.Collate(c.Name, "SQL_Latin1_General_CP1_CI_AS") == 
                EF.Functions.Collate(name, "SQL_Latin1_General_CP1_CI_AS"));
        }

        public async Task<(IEnumerable<Course> Items, int TotalCount)> GetPaginatedWithVideosAsync(
            int pageNumber,
            int pageSize
        )
        {
            var totalCount = await context.Courses.CountAsync();

            var items = await context
                .Courses.AsNoTracking()
                .Include(c => c.Videos)
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(Course course)
        {
            await context.Courses.AddAsync(course);
        }

        public void Update(Course course)
        {
            context.Courses.Update(course);
        }

        public void Delete(Course course)
        {
            context.Courses.Remove(course);
        }

        // SaveChangesAsync removido - UnitOfWork é responsável por persistir
    }
}
