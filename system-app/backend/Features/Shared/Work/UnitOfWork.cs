using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Base;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Shared.Work;

public class UnitOfWork(ApiDbContext context) : IUnitOfWork
{
    public async Task CommitAsync()
    {
        // Salva todas as alterações pendentes (Inserts e Updates dos Repositories) de uma vez
        await context.SaveChangesAsync();
    }

    public Task RollbackAsync()
    {
        // No EF Core, se você não chamar SaveChanges, nada é persistido.
        // Mas se quiser limpar o ChangeTracker em caso de erro, pode fazer assim:
        context.ChangeTracker.Clear();
        return Task.CompletedTask;
    }
}