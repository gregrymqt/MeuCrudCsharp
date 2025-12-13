using System;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels;

public class ChargebackSummaryViewModel
{
    public string Id { get; set; }
    public string Customer { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
    public string MercadoPagoUrl { get; set; }
}
