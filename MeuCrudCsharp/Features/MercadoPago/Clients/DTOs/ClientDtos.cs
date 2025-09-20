using System.Text.Json.Serialization;

// O namespace continua o mesmo, agrupando todos os DTOs do cliente.
namespace MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

/// <summary>
/// Requisição para vincular um novo cartão a um cliente.
/// </summary>
/// <param name="Token">Token do cartão gerado pelo provedor de pagamentos.</param>
public record CardRequestDto(
    [property: JsonPropertyName("token")] string? Token
);

/// <summary>
/// Resposta com os dados de um cartão armazenado no provedor de pagamentos.
/// </summary>
/// <param name="Id">Identificador do cartão no provedor.</param>
/// <param name="CustomerId">Identificador do cliente associado ao cartão.</param>
/// <param name="LastFourDigits">Últimos quatro dígitos do cartão.</param>
/// <param name="PaymentMethod">Informações do método de pagamento do cartão.</param>
public record CardResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("customer_id")] string? CustomerId,
    [property: JsonPropertyName("last_four_digits")] string? LastFourDigits,
    [property: JsonPropertyName("payment_method")] PaymentMethodDto? PaymentMethod
);

/// <summary>
/// Requisição para criação/atualização de um cliente no provedor de pagamentos.
/// </summary>
/// <param name="Email">E-mail do cliente.</param>
/// <param name="FirstName">Primeiro nome do cliente.</param>
public record CustomerRequestDto(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("first_name")] string? FirstName
);

/// <summary>
/// Resposta com os dados do cliente no provedor de pagamentos.
/// </summary>
/// <param name="Id">Identificador do cliente no provedor.</param>
/// <param name="Email">E-mail do cliente.</param>
/// <param name="FirstName">Primeiro nome do cliente.</param>
/// <param name="DateCreated">Data de criação do registro no provedor.</param>
public record CustomerResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("date_created")] DateTime DateCreated
);

/// <summary>
/// Informações sobre o método de pagamento do cartão.
/// </summary>
/// <param name="Name">Nome/label do método de pagamento (por exemplo, "Visa").</param>
public record PaymentMethodDto(
    [property: JsonPropertyName("name")] string? Name
);