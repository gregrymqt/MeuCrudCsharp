using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Profiles.Admin.Interfaces
{
    public interface ISubscriptionService
    {
        Task<string> SearchSubscriptionAsync(string searchParameter);
        Task<string> UpdateSubscriptionValueAsync(string id, UpdateSubscriptionValueDto dto);
        Task<string> UpdateSubscriptionStatusAsync(string id, UpdateSubscriptionStatusDto dto);
    }
}
