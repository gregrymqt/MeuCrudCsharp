using System.Text.Json;
using MercadoPago.Client.Customer;
using MercadoPago.Resource.Customer;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Services;

/// <summary>
/// Service de integração com a API do Mercado Pago para gerenciar Customers e Cards.
/// Usa o SDK oficial do Mercado Pago.
/// </summary>
public class ClientMercadoPagoService(
    IHttpClientFactory httpClientFactory,
    ILogger<ClientMercadoPagoService> logger)
    : MercadoPagoServiceBase(httpClientFactory, logger), IClientMercadoPagoService
{
    /// <summary>
    /// Cria um novo Customer no Mercado Pago.
    /// </summary>
    public async Task<Customer> CreateCustomerAsync(string email, string firstName)
    {
        var customerClient = new CustomerClient();
        var request = new CustomerRequest { Email = email, FirstName = firstName };
        return await customerClient.CreateAsync(request);
    }

    /// <summary>
    /// Adiciona um cartão a um Customer existente no Mercado Pago.
    /// </summary>
    public async Task<CustomerCard> AddCardAsync(string customerId, string cardToken)
    {
        var customerClient = new CustomerClient();
        var request = new CustomerCardCreateRequest { Token = cardToken };
        return await customerClient.CreateCardAsync(customerId, request);
    }

    /// <summary>
    /// Lista todos os cartões de um Customer no Mercado Pago.
    /// Converte do objeto do SDK para nosso DTO.
    /// </summary>
    public async Task<List<CardInCustomerResponseDto>> ListCardsAsync(string customerId)
    {
        var customerClient = new CustomerClient();

        // O SDK já retorna uma lista iterável de Cards
        var cards = await customerClient.ListCardsAsync(customerId);

        if (cards == null || !cards.Any())
            return [];

        // Mapeia do Objeto do SDK -> Nosso DTO
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

    /// <summary>
    /// Obtém um cartão específico de um Customer no Mercado Pago.
    /// </summary>
    public async Task<CardInCustomerResponseDto?> GetCardAsync(string customerId, string cardId)
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

    /// <summary>
    /// Remove um cartão de um Customer no Mercado Pago.
    /// Usa chamada HTTP direta pois o SDK às vezes falha nesta operação.
    /// </summary>
    public async Task<CardInCustomerResponseDto> DeleteCardAsync(string customerId, string cardId)
    {
        var endpoint = $"/v1/customers/{customerId}/cards/{cardId}";

        var responseBody = await SendMercadoPagoRequestAsync<object>(
            HttpMethod.Delete,
            endpoint,
            null
        );

        // Desserializa a resposta JSON manual
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<CardInCustomerResponseDto>(responseBody, options)
            ?? throw new AppServiceException("Falha ao desserializar resposta do MP.");
    }
}
