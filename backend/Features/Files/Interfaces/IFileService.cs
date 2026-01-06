using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Interfaces;

public interface IFileService
{
    Task<string?> ProcessChunkAsync(
        IFormFile chunk,
        string fileName,
        int chunkIndex,
        int totalChunks
    );

    Task<EntityFile> SalvarArquivoDoTempAsync(
        string tempPath,
        string nomeOriginal,
        string categoria
    );

    Task<EntityFile> SubstituirArquivoDoTempAsync(int fileId, string tempPath, string nomeOriginal);

    // Salva um novo arquivo no disco e no banco
    Task<EntityFile> SalvarArquivoAsync(IFormFile arquivo, string featureCategoria);

    // Substitui um arquivo existente (remove o antigo físico e atualiza metadados)
    Task<EntityFile> SubstituirArquivoAsync(int idArquivoAntigo, IFormFile novoArquivo);

    // Remove o arquivo do disco e do banco de dados
    Task DeletarArquivoAsync(int id);
}
