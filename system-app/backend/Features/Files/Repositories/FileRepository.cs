using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Repositories;

public class FileRepository(ApiDbContext context) : IFileRepository
{
    public async Task<EntityFile> GetByIdAsync(int id)
        => await context.Files.FindAsync(id);

    public async Task AddAsync(EntityFile arquivo)
    {
        await context.Files.AddAsync(arquivo);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
    }

    public Task UpdateAsync(EntityFile arquivo)
    {
        context.Files.Update(arquivo);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EntityFile arquivo)
    {
        context.Files.Remove(arquivo);
        // NÃO chama SaveChangesAsync - deixa pro UnitOfWork
        return Task.CompletedTask;
    }
}
