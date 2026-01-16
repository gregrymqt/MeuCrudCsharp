using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace MeuCrudCsharp.Features.Files.Utils;

public static class FilesHelper
{
    private static IFileRepository _repository;
    private static IWebHostEnvironment _environment;

    // Inicializa as dependências estáticas
    public static void Initialize(IFileRepository repository, IWebHostEnvironment environment)
    {
        _repository = repository;
        _environment = environment;
    }

    // MUDEI DE PRIVATE PARA PUBLIC
    public static string GerarCaminhoFisico(string featureCategoria, string nomeArquivo)
    {
        // Garante que o environment foi carregado
        if (_environment == null)
            throw new InvalidOperationException("FilesHelper não inicializado.");

        string pastaDestino = Path.Combine(_environment.WebRootPath, "uploads", featureCategoria);

        if (!Directory.Exists(pastaDestino))
        {
            Directory.CreateDirectory(pastaDestino);
        }

        return Path.Combine(pastaDestino, nomeArquivo);
    }

    // MUDEI DE PRIVATE PARA PUBLIC
    public static string ObterContentType(string nomeArquivo)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(nomeArquivo, out string contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType;
    }

    public static async Task<EntityFile> RegistrarNoBancoAsync(
        string nomeArquivo,
        string nomeOriginal,
        string featureCategoria
    )
    {
        if (_repository == null)
            throw new InvalidOperationException("FilesHelper não inicializado.");

        string caminhoFisico = GerarCaminhoFisico(featureCategoria, nomeArquivo);
        var fileInfo = new FileInfo(caminhoFisico);

        var novoArquivo = new EntityFile
        {
            NomeArquivo = nomeArquivo,
            FeatureCategoria = featureCategoria,
            ContentType = ObterContentType(nomeOriginal),
            TamanhoBytes = fileInfo.Length,
            CaminhoRelativo = Path.Combine("uploads", featureCategoria, nomeArquivo)
                .Replace("\\", "/"),
        };

        await _repository.AddAsync(novoArquivo);
        return novoArquivo;
    }
}
