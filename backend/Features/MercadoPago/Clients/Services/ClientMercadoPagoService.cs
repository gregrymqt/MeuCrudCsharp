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
    // Construtor repassa o HttpClient e Logger para a Base
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

    public async Task<List<CardInCustomerResponseDto>> ListCardsAsync(string customerId)
    {
        var customerClient = new CustomerClient();
        var response = await customerClient.ListCardsAsync(customerId);

        var jsonContent = response.ApiResponse.Content;
        if (string.IsNullOrEmpty(jsonContent))
            return new List<CardInCustomerResponseDto>();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<CardInCustomerResponseDto>>(jsonContent, options)
            ?? new List<CardInCustomerResponseDto>();
    }

    public async Task<CardInCustomerResponseDto> GetCardAsync(string customerId, string cardId)
    {
        var customerClient = new CustomerClient();
        var response = await customerClient.GetCardAsync(customerId, cardId);

        var jsonContent = response.ApiResponse.Content;
        if (string.IsNullOrEmpty(jsonContent))
            return null;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<CardInCustomerResponseDto>(jsonContent, options);
    }

    public async Task<CardInCustomerResponseDto> DeleteCardAsync(string customerId, string cardId)
    {
        // Aqui usamos o método manual da Base Class, pois o SDK as vezes tem limitações no delete
        var endpoint = $"/v1/customers/{customerId}/cards/{cardId}";

        var responseBody = await SendMercadoPagoRequestAsync(
            HttpMethod.Delete,
            endpoint,
            (object?)null
        );

        return JsonSerializer.Deserialize<CardInCustomerResponseDto>(responseBody)
            ?? throw new AppServiceException("Falha ao desserializar resposta do MP.");
    }
}
