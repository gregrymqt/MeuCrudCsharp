using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;

public interface IClaimRepository
{
    Task<(List<Models.Claims> Claims, int TotalCount)> GetPaginatedClaimsAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize
    );
    Task<Models.Claims?> GetByIdAsync(long id);
    Task UpdateClaimStatusAsync(Models.Claims claim, string newStatus);
    Task<List<Models.Claims>> GetClaimsByUserIdAsync(string userId);
}
