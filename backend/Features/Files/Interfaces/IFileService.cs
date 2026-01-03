using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Interfaces;

public interface IFileService
{
    // Salva um novo arquivo no disco e no banco
    Task<EntityFile> SalvarArquivoAsync(IFormFile arquivo, string featureCategoria);

    // Substitui um arquivo existente (remove o antigo físico e atualiza metadados)
    Task<EntityFile> SubstituirArquivoAsync(int idArquivoAntigo, IFormFile novoArquivo);

    // Remove o arquivo do disco e do banco de dados
    Task DeletarArquivoAsync(int id);
}
