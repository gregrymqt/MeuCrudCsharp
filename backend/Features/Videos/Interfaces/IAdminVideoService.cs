using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Videos.Interfaces
{
    public interface IAdminVideoService
    {
        /// <summary>
        /// Realiza o upload do vídeo e thumbnail (usando UploadService) e cria o registro no banco.
        /// </summary>
        Task<Video> HandleVideoUploadAsync(IFormFile videoFile, string title, string description, IFormFile? thumbnailFile);

        /// <summary>
        /// Atualiza os dados de um vídeo existente.
        /// </summary>
        Task<Video> UpdateVideoAsync(Guid id, UpdateVideoDto dto);

        /// <summary>
        /// Remove o vídeo do banco e apaga seus arquivos físicos.
        /// </summary>
        Task DeleteVideoAsync(Guid id);

        /// <summary>
        /// Retorna a lista paginada de vídeos.
        /// </summary>
        Task<PaginatedResultDto<VideoDto>> GetAllVideosAsync(int page, int pageSize);
    }
}
