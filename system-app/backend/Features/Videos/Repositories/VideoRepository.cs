using System;
using System.Collections.Generic;
using System.Linq; // Necessário para Skip/Take
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Videos.Repositories
{
    public class VideoRepository : IVideoRepository
    {
        private readonly ApiDbContext _context;

        public VideoRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<Video> GetByIdAsync(int id)
        {
            // CORREÇÃO: Include deve apontar para a Entidade Relacionada (tabela Files), não para uma string.
            // Assumindo que sua Model Video tem: public virtual EntityFile File { get; set; }
            return await _context.Videos.Include(v => v.File).FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Video> GetByStorageIdentifierAsync(string storageId)
        {
            return await _context
                .Videos.Include(v => v.File) // É bom trazer o arquivo aqui também se for usar o path
                .FirstOrDefaultAsync(v => v.StorageIdentifier == storageId);
        }

        public async Task AddAsync(Video video)
        {
            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Video video)
        {
            _context.Videos.Update(video);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int videoId, VideoStatus newStatus)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video != null)
            {
                video.Status = newStatus;
                await _context.SaveChangesAsync();
            }
        }

        // IMPLEMENTAÇÃO DA PAGINAÇÃO
        public async Task<(List<Video> Items, int TotalCount)> GetAllPaginatedAsync(
            int page,
            int pageSize
        )
        {
            var query = _context.Videos.AsNoTracking(); // AsNoTracking melhora performance para leitura

            // 1. Contagem total para o frontend saber quantas páginas existem
            var totalCount = await query.CountAsync();

            // 2. Busca paginada (Sempre ordene antes de paginar!)
            var items = await query
                .OrderByDescending(v => v.UploadDate) // Ordenar do mais novo para o mais antigo
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(v => v.File) // Opcional: trazer dados do arquivo na listagem
                .ToListAsync();

            return (items, totalCount);
        }

        // IMPLEMENTAÇÃO DA BUSCA POR GUID (PUBLIC ID)
        public async Task<Video> GetByPublicIdAsync(Guid publicId)
        {
            // Assumindo que sua Model tem uma propriedade 'Guid Id' ou 'Guid PublicId'
            // Se o ID principal for int, você deve ter criado um campo 'PublicId' na model.

            // Opção A: Se o ID da tabela JÁ É o Guid
            // return await _context.Videos.Include(v => v.File).FirstOrDefaultAsync(v => v.Id == publicId);

            // Opção B: Se o ID é int, mas existe uma coluna PublicId (Recomendado para segurança)
            return await _context
                .Videos.Include(v => v.File)
                .FirstOrDefaultAsync(v => v.PublicId == publicId);
        }

        // IMPLEMENTAÇÃO DO DELETE
        public async Task DeleteAsync(Video video)
        {
            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();
        }
    }
}
