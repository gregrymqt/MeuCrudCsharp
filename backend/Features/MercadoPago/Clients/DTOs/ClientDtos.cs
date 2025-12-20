using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;

// --- BACK-END / MP (Records Imutáveis) ---

public record CardInCustomerResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("last_four_digits")] string? LastFourDigits,
    [property: JsonPropertyName("expiration_month")] int? ExpirationMonth,
    [property: JsonPropertyName("expiration_year")] int? ExpirationYear,
    // Adicionado para o Front saber a bandeira (visa, master)
    [property: JsonPropertyName("payment_method")] PaymentMethodDto? PaymentMethod
);

// O MP geralmente retorna o método de pagamento dentro de um objeto ou como id direto.
// Se o seu JSON retornar direto o ID, mude para string. Se for objeto:
public record PaymentMethodDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name
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
    public string CardToken { get; set; }
}
