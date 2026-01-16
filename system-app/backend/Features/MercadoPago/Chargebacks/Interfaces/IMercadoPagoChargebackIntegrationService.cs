using System;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;

public interface IMercadoPagoChargebackIntegrationService
{
    Task<MercadoPagoChargebackResponse?> GetChargebackDetailsFromApiAsync(string chargebackId);
}
