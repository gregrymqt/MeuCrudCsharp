using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.DTOs;

/// <summary>
/// Representa os dados necessários para solicitar o reembolso de um pagamento.
/// </summary>
/// <param name="Amount">
/// O valor a ser reembolsado. Se nulo, um reembolso total será tentado.
/// </param>
public record RefundRequestDto(
    [property: JsonPropertyName("amount")]
    [Range(
        typeof(decimal),
        "0.01",
        "1000000.00",
        ErrorMessage = "Se fornecido, o valor do reembolso deve ser positivo."
    )]
    decimal? Amount
);

/// <summary>
/// Representa a resposta de uma operação de reembolso do provedor de pagamentos.
/// </summary>
public record RefundResponseDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("payment_id")] long PaymentId,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("date_created")] DateTime DateCreated
);