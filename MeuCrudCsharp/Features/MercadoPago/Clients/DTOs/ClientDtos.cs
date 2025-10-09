using System.Text.Json.Serialization;

// O namespace continua o mesmo, agrupando todos os DTOs do cliente.
namespace MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

public record CardInCustomerResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("last_four_digits")]
    string? LastFourDigits,
    [property: JsonPropertyName("expiration_month")]
    int? ExpirationMonth,
    [property: JsonPropertyName("expiration_year")]
    int? ExpirationYear
);

public record CustomerWithCardResponseDto(
    [property: JsonPropertyName("id")] string? CustomerId,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("cards")] CardInCustomerResponseDto? Card
);

public record CardRequestDto(
    [property: JsonPropertyName("id")] string? Token
);