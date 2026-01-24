using MeuCrudCsharp.Data;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Shared.Work;

/// <summary>
/// Implementação do padrão Unit of Work para gerenciar transações do Entity Framework Core.
/// Centraliza a lógica de commit e rollback de mudanças no banco de dados.
/// </summary>
public class UnitOfWork(ApiDbContext context) : IUnitOfWork
{
    /// <summary>
    /// Persiste todas as alterações pendentes (Inserts, Updates, Deletes) de uma vez só.
    /// Garante atomicidade: ou todas as operações são salvas ou nenhuma é.
    /// </summary>
    public async Task CommitAsync()
    {
        try
        {
            // Salva todas as alterações pendentes (Inserts e Updates dos Repositories) de uma vez
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Log específico para erros de atualização do banco
            // Você pode adicionar um ILogger aqui se necessário
            throw new InvalidOperationException(
                "Erro ao persistir alterações no banco de dados. Verifique os logs para mais detalhes.",
                ex
            );
        }
    }

    /// <summary>
    /// Descarta todas as mudanças pendentes que ainda não foram persistidas.
    /// No EF Core, se você não chamar SaveChanges, nada é persistido automaticamente.
    /// </summary>
    public Task RollbackAsync()
    {
        // No EF Core, se você não chamar SaveChanges, nada é persistido.
        // Mas se quiser limpar o ChangeTracker em caso de erro, pode fazer assim:
        context.ChangeTracker.Clear();
        return Task.CompletedTask;
    }
}