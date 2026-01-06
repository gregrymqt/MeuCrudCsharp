using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Features.Files.Utils; // Importante para usar o Helper
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _repository;
    private readonly IWebHostEnvironment _environment;

    public FileService(IFileRepository repository, IWebHostEnvironment environment)
    {
        _repository = repository;
        _environment = environment;

        // Inicializa o Helper com as dependências deste escopo
        FilesHelper.Initialize(_repository, _environment);
    }

    // =========================================================================
    // LÓGICA DE CHUNKS (PROCESSAMENTO) - Mantém a lógica local
    // =========================================================================
    public async Task<string?> ProcessChunkAsync(
        IFormFile chunk,
        string fileName,
        int chunkIndex,
        int totalChunks
    )
    {
        var tempFolderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "temp-chunks"
        );

        if (!Directory.Exists(tempFolderPath))
            Directory.CreateDirectory(tempFolderPath);

        var tempFilePath = Path.Combine(tempFolderPath, fileName);

        using (var stream = new FileStream(tempFilePath, FileMode.Append))
        {
            await chunk.CopyToAsync(stream);
        }

        if (chunkIndex == totalChunks - 1)
        {
            return tempFilePath;
        }

        return null;
    }

    // =========================================================================
    // SALVAR ARQUIVOS (TEMP -> FINAL)
    // =========================================================================

    public async Task<EntityFile> SalvarArquivoDoTempAsync(
        string tempPath,
        string nomeOriginal,
        string categoria
    )
    {
        try
        {
            if (!File.Exists(tempPath))
                throw new FileNotFoundException("Arquivo temporário não encontrado.", tempPath);

            string novoNome = $"{Guid.NewGuid()}_{nomeOriginal}";

            // Usa o Helper para pegar o caminho correto
            string caminhoFinalFisico = FilesHelper.GerarCaminhoFisico(categoria, novoNome);

            // Move o arquivo
            File.Move(tempPath, caminhoFinalFisico);

            // Usa o Helper para salvar no banco (ele já cria a EntityFile e salva)
            return await FilesHelper.RegistrarNoBancoAsync(novoNome, nomeOriginal, categoria);
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw new AppServiceException("Erro ao salvar arquivo remontado.", ex);
        }
    }

    public async Task<EntityFile> SubstituirArquivoDoTempAsync(
        int fileId,
        string tempPath,
        string nomeOriginal
    )
    {
        try
        {
            var arquivoBanco = await _repository.GetByIdAsync(fileId);
            if (arquivoBanco == null)
                throw new ResourceNotFoundException($"Arquivo ID {fileId} não encontrado.");

            // Apaga arquivo físico antigo
            string caminhoAntigoFisico = Path.Combine(
                _environment.WebRootPath,
                arquivoBanco.CaminhoRelativo
            );
            if (File.Exists(caminhoAntigoFisico))
            {
                File.Delete(caminhoAntigoFisico);
            }

            // Gera dados do novo arquivo
            string novoNome = $"{Guid.NewGuid()}_{nomeOriginal}";
            string novoCaminhoFisico = FilesHelper.GerarCaminhoFisico(
                arquivoBanco.FeatureCategoria,
                novoNome
            );

            // Move do Temp
            if (File.Exists(tempPath))
            {
                File.Move(tempPath, novoCaminhoFisico);
            }
            else
            {
                throw new FileNotFoundException("Arquivo temporário sumiu antes de mover.");
            }

            // Atualiza Objeto no Banco (Update Manual, pois o Helper só faz Add)
            var fileInfo = new FileInfo(novoCaminhoFisico);

            arquivoBanco.NomeArquivo = novoNome;
            arquivoBanco.ContentType = FilesHelper.ObterContentType(nomeOriginal); // Reusa o Helper
            arquivoBanco.TamanhoBytes = fileInfo.Length;
            arquivoBanco.CaminhoRelativo = Path.Combine(
                    "uploads",
                    arquivoBanco.FeatureCategoria,
                    novoNome
                )
                .Replace("\\", "/");

            await _repository.UpdateAsync(arquivoBanco);
            return arquivoBanco;
        }
        catch (Exception ex)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw new AppServiceException(
                $"Erro ao substituir pelo arquivo remontado ID {fileId}.",
                ex
            );
        }
    }

    // =========================================================================
    // MÉTODOS ORIGINAIS (UPLOAD DIRETO PEQUENO)
    // =========================================================================

    public async Task<EntityFile> SalvarArquivoAsync(IFormFile arquivo, string featureCategoria)
    {
        try
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new AppServiceException("Nenhum arquivo enviado.");

            string nomeArquivo = $"{Guid.NewGuid()}_{arquivo.FileName}";

            // Usa o Helper para caminho
            string caminhoFisico = FilesHelper.GerarCaminhoFisico(featureCategoria, nomeArquivo);

            using (var stream = new FileStream(caminhoFisico, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            // Usa o Helper para registrar no banco
            return await FilesHelper.RegistrarNoBancoAsync(
                nomeArquivo,
                arquivo.FileName,
                featureCategoria
            );
        }
        catch (Exception ex)
        {
            throw new AppServiceException("Falha ao salvar arquivo direto.", ex);
        }
    }

    public async Task<EntityFile> SubstituirArquivoAsync(int idArquivoAntigo, IFormFile novoArquivo)
    {
        var arquivoBanco = await _repository.GetByIdAsync(idArquivoAntigo);
        if (arquivoBanco == null)
            throw new ResourceNotFoundException("Arquivo antigo não encontrado.");

        // Remove antigo
        string caminhoAntigo = Path.Combine(_environment.WebRootPath, arquivoBanco.CaminhoRelativo);
        if (File.Exists(caminhoAntigo))
            File.Delete(caminhoAntigo);

        // Salva novo
        string novoNome = $"{Guid.NewGuid()}_{novoArquivo.FileName}";
        string novoCaminho = FilesHelper.GerarCaminhoFisico(
            arquivoBanco.FeatureCategoria,
            novoNome
        );

        using (var stream = new FileStream(novoCaminho, FileMode.Create))
        {
            await novoArquivo.CopyToAsync(stream);
        }

        // Atualiza Dados
        arquivoBanco.NomeArquivo = novoNome;
        arquivoBanco.ContentType = novoArquivo.ContentType;
        arquivoBanco.TamanhoBytes = novoArquivo.Length;
        arquivoBanco.CaminhoRelativo = Path.Combine(
                "uploads",
                arquivoBanco.FeatureCategoria,
                novoNome
            )
            .Replace("\\", "/");

        await _repository.UpdateAsync(arquivoBanco);
        return arquivoBanco;
    }

    public async Task DeletarArquivoAsync(int id)
    {
        var arquivo = await _repository.GetByIdAsync(id);
        if (arquivo != null)
        {
            string path = Path.Combine(_environment.WebRootPath, arquivo.CaminhoRelativo);
            if (File.Exists(path))
                File.Delete(path);
            await _repository.DeleteAsync(arquivo);
        }
    }
}
