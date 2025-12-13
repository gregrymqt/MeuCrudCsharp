using System;
using System.Collections.Generic;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;

public class ClaimsIndexViewModel
{
    /// <summary>
    /// Lista de reclamações a serem exibidas na página atual.
    /// </summary>
    public List<ClaimSummaryViewModel> Claims { get; set; } = new();

    // --- Propriedades para os filtros de busca ---
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // --- Informações de paginação ---
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
