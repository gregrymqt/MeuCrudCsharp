using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;

public interface IChargebackRepository
{
    Task<(List<Chargeback> Chargebacks, int TotalCount)> GetPaginatedChargebacksAsync(
        string? searchTerm,
        string? statusFilter,
        int page,
        int pageSize);
}