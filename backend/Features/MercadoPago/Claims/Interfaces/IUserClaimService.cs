using System;
using static MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels.MercadoPagoClaimsViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;

public interface IUserClaimService
{
    Task<List<ClaimSummaryViewModel>> GetMyClaimsAsync();
    Task<ClaimDetailViewModel> GetMyClaimDetailAsync(int internalId);
    Task ReplyAsync(int internalId, string message);
    Task RequestMediationAsync(int internalId);
}
