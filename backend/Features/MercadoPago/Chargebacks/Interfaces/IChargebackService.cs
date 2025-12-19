using System.Threading.Tasks;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels;
using static MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels.ChargeBackViewModels;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;

public interface IChargebackService
{
    Task<ChargebacksIndexViewModel> GetChargebacksAsync(
        string? searchTerm,
        string? statusFilter,
        int page
    );

    Task<ChargebackDetailViewModel> GetChargebackDetailAsync(string chargebackId);
}
