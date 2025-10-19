using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;

public interface IClaimService
{
    Task<ClaimsIndexViewModel> GetClaimsAsync(string? searchTerm, string? statusFilter, int page);
    Task UpdateClaimStatusAsync(long id, string newStatus);
}
