using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Plans.Interfaces
{
    public interface IPlanService
    {
        Task<List<PlanDto>> GetActivePlansAsync();
        Task<Plan> CreatePlanAsync(CreatePlanDto createDto);
        Task<Plan> UpdatePlanAsync(string externalPlanId, UpdatePlanDto updateDto); // Assinatura corrigida
        Task DeletePlanAsync(string externalPlanId);
    }
}
