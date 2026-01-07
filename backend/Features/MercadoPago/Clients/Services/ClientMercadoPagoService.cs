using System;
using System.Text.Json;
using MercadoPago.Client.Customer;
using MercadoPago.Resource.Customer;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Services;

public class ClientMercadoPagoService : MercadoPagoServiceBase, IClientMercadoPagoService
{
    public ClientMercadoPagoService(
        IHttpClientFactory httpClientFactory,
        ILogger<ClientMercadoPagoService> logger
    )
        : base(httpClientFactory, logger) { }

    public async Task<Customer> CreateCustomerAsync(string email, string firstName)
    {
        var customerClient = new CustomerClient();
        var request = new CustomerRequest { Email = email, FirstName = firstName };
        return await customerClient.CreateAsync(request);
    }

    public async Task<CustomerCard> AddCardAsync(string customerId, string cardToken)
    {
        var customerClient = new CustomerClient();
        var request = new CustomerCardCreateRequest { Token = cardToken };
        return await customerClient.CreateCardAsync(customerId, request);
    }

    // --- CORREÇÃO AQUI: Usando o objeto do SDK em vez de JSON cru ---
    public async Task<List<CardInCustomerResponseDto>> ListCardsAsync(string customerId)
    {
        var customerClient = new CustomerClient();

        // O SDK já retorna uma lista iterável de Cards
        var cards = await customerClient.ListCardsAsync(customerId);

        if (cards == null || !cards.Any())
            return new List<CardInCustomerResponseDto>();

        // Mapeamos do Objeto do SDK -> Nosso DTO
        return cards
            .Select(c => new CardInCustomerResponseDto(
                c.Id,
                c.LastFourDigits,
                c.ExpirationMonth,
                c.ExpirationYear,
                new PaymentMethodDto(c.PaymentMethod?.Id, c.PaymentMethod?.Name)
            ))
            .ToList();
    }

    public async Task<CardInCustomerResponseDto> GetCardAsync(string customerId, string cardId)
    {
        var customerClient = new CustomerClient();
        var c = await customerClient.GetCardAsync(customerId, cardId);

        if (c == null)
            return null;

        return new CardInCustomerResponseDto(
            c.Id,
            c.LastFourDigits,
            c.ExpirationMonth,
            c.ExpirationYear,
            new PaymentMethodDto(c.PaymentMethod?.Id, c.PaymentMethod?.Name)
        );
    }

    public async Task<CardInCustomerResponseDto> DeleteCardAsync(string customerId, string cardId)
    {
        // Mantemos sua lógica manual aqui, pois o Delete do SDK as vezes falha
        var endpoint = $"/v1/customers/{customerId}/cards/{cardId}";

        var responseBody = await SendMercadoPagoRequestAsync(
            HttpMethod.Delete,
            endpoint,
            (object?)null
        );

        // Aqui tudo bem usar JSON manual pois é uma resposta raw do nosso BaseService
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<CardInCustomerResponseDto>(responseBody, options)
            ?? throw new AppServiceException("Falha ao desserializar resposta do MP.");
    }
}
