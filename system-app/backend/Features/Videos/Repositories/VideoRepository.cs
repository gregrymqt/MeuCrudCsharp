using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Videos.Repositories
{
    /// <summary>
    /// Repository para gerenciar operações de persistência de Videos.
    /// Apenas marca as mudanças no DbContext - NÃO persiste diretamente.
    /// O Service é responsável por chamar UnitOfWork.CommitAsync().
    /// </summary>
    public class VideoRepository : IVideoRepository
    {
        private readonly ApiDbContext _context;

        public VideoRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<Video> GetByIdAsync(int id)
        {
            return await _context.Videos.Include(v => v.File).FirstOrDefaultAsync(v => v.Id == id) ??
                   throw new InvalidOperationException("Vídeo não encontrado.");
        }

        public async Task<Video> GetByStorageIdentifierAsync(string storageId)
        {
            return await _context
                .Videos.Include(v => v.File)
                .FirstOrDefaultAsync(v => v.StorageIdentifier == storageId) ?? 
                throw new InvalidOperationException("Vídeo não encontrado.");
        }

        /// <summary>
        /// Marca um vídeo para adição.
        /// NÃO persiste - O Service chamará UnitOfWork.CommitAsync().
        /// </summary>
        public async Task AddAsync(Video video)
        {
            await _context.Videos.AddAsync(video);
            // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
        }

        /// <summary>
        /// Marca um vídeo para atualização.
        /// NÃO persiste - O Service chamará UnitOfWork.CommitAsync().
        /// </summary>
        public async Task UpdateAsync(Video video)
        {
            _context.Videos.Update(video);
            // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
            await Task.CompletedTask; // Para manter assinatura async
        }

        /// <summary>
        /// Marca um vídeo para atualização de status.
        /// NÃO persiste - O Service chamará UnitOfWork.CommitAsync().
        /// </summary>
        public async Task UpdateStatusAsync(int videoId, VideoStatus newStatus)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video != null)
            {
                video.Status = newStatus;
                // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
            }
        }

        // IMPLEMENTAÇÃO DA PAGINAÇÃO (apenas leitura)
        public async Task<(List<Video> Items, int TotalCount)> GetAllPaginatedAsync(
            int page,
            int pageSize
        )
        {
            var query = _context.Videos.AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(v => v.UploadDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(v => v.File)
                .ToListAsync();

            return (items, totalCount);
        }

        // IMPLEMENTAÇÃO DA BUSCA POR GUID (PUBLIC ID)
        public async Task<Video> GetByPublicIdAsync(Guid publicId)
        {
            return await _context
                .Videos.Include(v => v.File)
                .FirstOrDefaultAsync(v => v.PublicId == publicId) ?? 
                throw new InvalidOperationException("Vídeo não encontrado.");
        }

        /// <summary>
        /// Marca um vídeo para remoção.
        /// NÃO persiste - O Service chamará UnitOfWork.CommitAsync().
        /// </summary>
        public async Task DeleteAsync(Video video)
        {
            _context.Videos.Remove(video);
            // NÃO chama SaveChanges - deixa pro Service via UnitOfWork
            await Task.CompletedTask; // Para manter assinatura async
        }
    }
}