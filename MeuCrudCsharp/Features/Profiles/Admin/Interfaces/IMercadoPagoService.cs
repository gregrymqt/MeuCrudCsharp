using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Profiles.Admin.Interfaces
{
    public interface IMercadoPagoService
    {
        Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto planDto);
    }
}
