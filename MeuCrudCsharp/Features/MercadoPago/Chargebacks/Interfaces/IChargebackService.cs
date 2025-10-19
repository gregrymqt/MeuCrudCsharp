using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;

public interface IChargebackService
{
    Task<ChargebacksIndexViewModel> GetChargebacksAsync(
        string? searchTerm,
        string? statusFilter,
        int page
    );
}
