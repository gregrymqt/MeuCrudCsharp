using System;

namespace MeuCrudCsharp.Features.Shared.Work;

public interface IUnitOfWork
{
    Task CommitAsync();
    Task RollbackAsync(); // Opcional se usar apenas try/catch
}
