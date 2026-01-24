// Em Features/MercadoPago/Payments/Services/MercadoPagoPaymentService.cs
using System.Text.Json;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class MercadoPagoPaymentService(
        IHttpClientFactory httpClient,
        ILogger<MercadoPagoPaymentService> logger)
        : MercadoPagoServiceBase(httpClient, logger), IMercadoPagoPaymentService
    {
        // O construtor apenas passa as dependências para a classe base

        public async Task<MercadoPagoPaymentDetails?> GetPaymentStatusAsync(
            string externalPaymentId
        )
        {
            logger.LogInformation(
                "Buscando status do pagamento {PaymentId} no Mercado Pago.",
                externalPaymentId
            );

            var endpoint = $"/v1/payments/{externalPaymentId}";

            // Usando o método da sua classe base para uma requisição GET (sem corpo/payload)
            var responseJson = await SendMercadoPagoRequestAsync<object>(
                HttpMethod.Get,
                endpoint,
                payload: null // Não enviamos corpo em uma requisição GET
            );

            if (string.IsNullOrEmpty(responseJson))
            {
                return null;
            }

            // Desserializa a resposta JSON para o nosso DTO
            var paymentDetails = JsonSerializer.Deserialize<MercadoPagoPaymentDetails>(
                responseJson
            );

            return paymentDetails;
        }
    }
}
