using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Courses.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly ApiDbContext _context;

        public CourseRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<Course?> GetByPublicIdAsync(Guid publicId)
        {
            return await _context.Courses.FirstOrDefaultAsync(c => c.PublicId == publicId);
        }

        public async Task<Course?> GetByPublicIdWithVideosAsync(Guid publicId)
        {
            return await _context
                .Courses.Include(c => c.Videos) // Carrega os vídeos (usado no DeleteCourseAsync)
                .FirstOrDefaultAsync(c => c.PublicId == publicId);
        }

        public async Task<Course?> GetByNameAsync(string name)
        {
            return await _context.Courses.FirstOrDefaultAsync(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            );
        }

        public async Task<IEnumerable<Course>> SearchByNameAsync(string name)
        {
            return await _context
                .Courses.AsNoTracking()
                .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Courses.AnyAsync(c => c.Name == name);
        }

        public async Task<(IEnumerable<Course> Items, int TotalCount)> GetPaginatedWithVideosAsync(
            int pageNumber,
            int pageSize
        )
        {
            var totalCount = await _context.Courses.CountAsync();

            var items = await _context
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
            await _context.Courses.AddAsync(course);
        }

        public void Update(Course course)
        {
            _context.Courses.Update(course);
        }

        public void Delete(Course course)
        {
            _context.Courses.Remove(course);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
