using System;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Files.Services;

public class UploadService
    {
        private readonly IFileRepository _repository;
        private readonly IWebHostEnvironment _environment;

        public UploadService(IFileRepository repository, IWebHostEnvironment environment)
        {
            _repository = repository;
            _environment = environment;
        }

        // Gera o caminho físico baseado na Feature solicitada (Videos, Images, etc)
        private string GerarCaminhoFisico(string featureCategoria, string nomeArquivo)
        {
           // Regra absoluta: Tudo fica dentro de "uploads" [cite: 3]
            // Regra dinâmica: Subpasta é a featureCategoria
            string pastaDestino = Path.Combine(_environment.WebRootPath, "uploads", featureCategoria);

            if (!Directory.Exists(pastaDestino))
            {
                Directory.CreateDirectory(pastaDestino); 
            }

            return Path.Combine(pastaDestino, nomeArquivo);
        }

        public async Task<EntityFile> SalvarArquivoAsync(IFormFile arquivo, string featureCategoria)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    throw new AppServiceException("Nenhum arquivo foi enviado para upload."); 
                string nomeArquivo = $"{Guid.NewGuid()}_{arquivo.FileName}";
                string caminhoFisico = GerarCaminhoFisico(featureCategoria, nomeArquivo);

                using (var stream = new FileStream(caminhoFisico, FileMode.Create))
                {
                    await arquivo.CopyToAsync(stream);
                }

                // Cria o registro no banco
                var novoArquivo = new EntityFile
                {
                    NomeArquivo = nomeArquivo,
                    FeatureCategoria = featureCategoria,
                    ContentType = arquivo.ContentType,
                    TamanhoBytes = arquivo.Length,
                    // Caminho relativo para acessar via URL depois
                    CaminhoRelativo = Path.Combine("uploads", featureCategoria, nomeArquivo)
                };

                await _repository.AddAsync(novoArquivo); 
                return novoArquivo;
            }
            catch (Exception ex) when (!(ex is AppServiceException))
            {
                throw new AppServiceException("Falha ao processar o upload do arquivo.", ex); 
            }
        }

        // Método genérico de Update: Remove o antigo e põe o novo
        public async Task<EntityFile> SubstituirArquivoAsync(int idArquivoAntigo, IFormFile novoArquivo)
        {
            try
            {
                // 1. Validar existência do arquivo antigo
                var arquivoBanco = await _repository.GetByIdAsync(idArquivoAntigo);
                if (arquivoBanco == null)
                    throw new ResourceNotFoundException($"Arquivo ID {idArquivoAntigo} não encontrado para substituição."); 

                if (novoArquivo != null && novoArquivo.Length > 0)
                {
                    // 2. Apagar fisicamente o arquivo antigo (Regra solicitada)
                    string caminhoAntigoFisico = Path.Combine(_environment.WebRootPath, arquivoBanco.CaminhoRelativo);
                    if (File.Exists(caminhoAntigoFisico))
                    {
                        File.Delete(caminhoAntigoFisico); 
                    }

                    // 3. Preparar e Salvar o novo arquivo
                    // Nota: Mantemos a mesma categoria (pasta) original do arquivo
                    string novoNome = $"{Guid.NewGuid()}_{novoArquivo.FileName}";
                    string novoCaminhoFisico = GerarCaminhoFisico(arquivoBanco.FeatureCategoria, novoNome);

                    using (var stream = new FileStream(novoCaminhoFisico, FileMode.Create))
                    {
                        await novoArquivo.CopyToAsync(stream); 
                    }

                    // 4. Atualizar metadados no objeto existente
                    arquivoBanco.NomeArquivo = novoNome;
                    arquivoBanco.ContentType = novoArquivo.ContentType;
                    arquivoBanco.TamanhoBytes = novoArquivo.Length;
                    arquivoBanco.CaminhoRelativo = Path.Combine("uploads", arquivoBanco.FeatureCategoria, novoNome);

                    // 5. Atualizar no banco
                    await _repository.UpdateAsync(arquivoBanco);
                }

                return arquivoBanco;
            }
            catch (ResourceNotFoundException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                throw new AppServiceException($"Erro ao substituir o arquivo ID {idArquivoAntigo}.", ex); 
            }
        }

        public async Task DeletarArquivoAsync(int id)
        {
            var arquivo = await _repository.GetByIdAsync(id);
            if (arquivo == null)
                throw new ResourceNotFoundException("Arquivo não encontrado para deleção."); 

            try
            {
                // Remove do disco
                string caminhoFisico = Path.Combine(_environment.WebRootPath, arquivo.CaminhoRelativo);
                if (File.Exists(caminhoFisico))
                {
                    File.Delete(caminhoFisico);
                }

                // Remove do banco
                await _repository.DeleteAsync(arquivo); 
            }
            catch (Exception ex)
            {
                throw new AppServiceException("Erro crítico ao deletar arquivo.", ex); 
        }
    }
    }