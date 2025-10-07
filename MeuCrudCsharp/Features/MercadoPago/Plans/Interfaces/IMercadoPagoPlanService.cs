using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;

public interface IMercadoPagoPlanService
{
    Task<PlanResponseDto> CreatePlanAsync(object payload);
    Task<PlanResponseDto> UpdatePlanAsync(string externalPlanId, object payload);
    Task CancelPlanAsync(string externalPlanId);
    Task<IEnumerable<PlanResponseDto>> SearchActivePlansAsync();
}