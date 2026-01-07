using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

// --- BACK-END / MP (Records Imutáveis) ---

public record PaymentMethodDto(string? Id, string? Name);

public record CardInCustomerResponseDto(
    string? Id,
    string? LastFourDigits,
    int? ExpirationMonth,
    int? ExpirationYear,
    PaymentMethodDto? PaymentMethod
);

public record CustomerWithCardResponseDto(
    [property: JsonPropertyName("id")] string? CustomerId,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("cards")] CardInCustomerResponseDto? Card
);

public record CardRequestDto([property: JsonPropertyName("id")] string? Token);

// --- FRONT-END / WALLET (Classes para o React) ---

public class WalletCardDto
{
    public string Id { get; set; }
    public string LastFourDigits { get; set; }
    public int ExpirationMonth { get; set; }
    public int ExpirationYear { get; set; }
    public string PaymentMethodId { get; set; } // O Front recebe "visa", "master"
    public bool IsSubscriptionActiveCard { get; set; }
}

public class AddCardRequestDto
{
    [Required(ErrorMessage = "O token do cartão é obrigatório.")]
    public string CardToken { get; set; }
}
