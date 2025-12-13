using System;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;

public class ClaimSummaryViewModel
{
    public long Id { get; set; }
    public string OrderId { get; set; }
    public string CustomerName { get; set; }
    public string Status { get; set; }
    public DateTime DateCreated { get; set; }
    public string Reason { get; set; }
}
