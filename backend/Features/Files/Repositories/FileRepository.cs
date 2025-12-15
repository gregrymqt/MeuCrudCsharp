using System;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Repositories;

public class FileRepository : IFileRepository
{
    private readonly ApiDbContext _context;

        public FileRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<EntityFile> GetByIdAsync(int id)
            => await _context.Files.FindAsync(id);

        public async Task AddAsync(EntityFile arquivo)
        {
            await _context.Files.AddAsync(arquivo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EntityFile arquivo)
        {
            _context.Files.Update(arquivo);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(EntityFile arquivo)
        {
            _context.Files.Remove(arquivo);
            await _context.SaveChangesAsync();
        }
    }
