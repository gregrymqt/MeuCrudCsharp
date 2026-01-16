using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Interfaces;

public interface IFileRepository
{
    Task<EntityFile> GetByIdAsync(int id);
    Task AddAsync(EntityFile arquivo);
    Task UpdateAsync(EntityFile arquivo);
    Task DeleteAsync(EntityFile arquivo);
}
