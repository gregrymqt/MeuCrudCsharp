using System;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Videos.Interfaces;

public interface IVideoRepository
    {
        Task<Video> GetByIdAsync(int id);
        Task<Video> GetByStorageIdentifierAsync(string storageId);
        Task AddAsync(Video video);
        Task UpdateAsync(Video video);
        Task UpdateStatusAsync(int videoId, VideoStatus newStatus);
        Task<(List<Video> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize);
        Task<Video> GetByPublicIdAsync(Guid publicId);
        Task DeleteAsync(Video video);
    }
