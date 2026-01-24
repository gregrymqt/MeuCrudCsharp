using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.Services;

public class MercadoPagoChargebackIntegrationService(
    IHttpClientFactory httpClientFactory,
    ILogger<MercadoPagoChargebackIntegrationService> logger)
    : MercadoPagoServiceBase(httpClientFactory, logger),
        IMercadoPagoChargebackIntegrationService
{
    public async Task<MercadoPagoChargebackResponse?> GetChargebackDetailsFromApiAsync(
        string chargebackId
    )
    {
        // Endpoint: v1/chargebacks/{id}
        var endpoint = $"v1/chargebacks/{chargebackId}";

        try
        {
            // Reutilizando seu método genérico da Base Class
            // Passamos 'object' no payload porque GET não tem corpo
            var jsonResponse = await SendMercadoPagoRequestAsync<object>(
                HttpMethod.Get,
                endpoint,
                null
            );

            if (string.IsNullOrEmpty(jsonResponse))
                return null;

            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

            return JsonSerializer.Deserialize<MercadoPagoChargebackResponse>(jsonResponse, options);
        }
        catch (ExternalApiException)
        {
            // O Base Service já logou o erro externo, apenas repassamos
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha de deserialização ou erro interno ao buscar chargeback {Id}",
                chargebackId
            );
            throw new AppServiceException("Erro ao processar resposta do Mercado Pago.", ex);
        }
    }
}