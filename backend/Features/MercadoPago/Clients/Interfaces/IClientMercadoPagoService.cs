using System;
using MercadoPago.Resource.Customer;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;

public interface IClientMercadoPagoService
{
    Task<Customer> CreateCustomerAsync(string email, string firstName);
    Task<CustomerCard> AddCardAsync(string customerId, string cardToken);
    Task<List<CardInCustomerResponseDto>> ListCardsAsync(string customerId);
    Task<CardInCustomerResponseDto> GetCardAsync(string customerId, string cardId);
    Task<CardInCustomerResponseDto> DeleteCardAsync(string customerId, string cardId);
}
