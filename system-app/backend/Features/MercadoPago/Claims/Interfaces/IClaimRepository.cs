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
    Task<Models.Claims?> GetByMpClaimIdAsync(long mpClaimId);
    Task<bool> ExistsByMpClaimIdAsync(long mpClaimId);
    Task<List<Models.Claims>> GetClaimsByUserIdAsync(string userId);
    
    // Métodos de escrita (não chamam SaveChanges)
    Task AddAsync(Models.Claims claim);
    void Update(Models.Claims claim);
    void UpdateClaimStatus(Models.Claims claim, InternalClaimStatus newStatus);
}
