namespace MeuCrudCsharp.Features.Shared.Work;

/// <summary>
/// Interface para implementação do padrão Unit of Work.
/// Gerencia transações e persistência de mudanças no banco de dados.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste todas as mudanças pendentes no banco de dados.
    /// </summary>
    Task CommitAsync();
    
    /// <summary>
    /// Desfaz as mudanças pendentes (opcional - no EF Core, não chamar SaveChanges já previne persistência).
    /// </summary>
    Task RollbackAsync();
}
