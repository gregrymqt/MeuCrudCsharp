using System.Collections.Generic;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels;

public class ChargebacksIndexViewModel
{
    public List<ChargebackSummaryViewModel> Chargebacks { get; set; } = new();

    // Filtros
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }

    // Paginação
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}