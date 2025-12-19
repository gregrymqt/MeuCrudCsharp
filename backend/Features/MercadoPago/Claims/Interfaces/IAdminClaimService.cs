using MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;
using static MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels.MercadoPagoClaimsViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Services;

public interface IAdminClaimService
{
    // Método de listagem (Inbox)
    Task<ClaimsIndexViewModel> GetClaimsAsync(string? searchTerm, string? statusFilter, int page);

    // Método de detalhes (Sala de Guerra)
    Task<ClaimDetailViewModel> GetClaimDetailsAsync(long localId);

    // Método de resposta
    Task ReplyToClaimAsync(long localId, string messageText);
}
