namespace MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Payments.Dtos; // Usado por SubscriptionWithCardRequestDto

/// <summary>
/// Representa os dados necessários para criar uma nova assinatura com o provedor de pagamentos.
/// </summary>
public record CreateSubscriptionDto(
    [property: JsonPropertyName("preapproval_plan_id")]
    [Required(ErrorMessage = "O ID do plano é obrigatório.")]
    string? PreapprovalPlanId,

    [property: JsonPropertyName("payer_email")]
    [Required(ErrorMessage = "O e-mail do pagador é obrigatório.")]
    [EmailAddress(ErrorMessage = "O e-mail do pagador deve ser um endereço válido.")]
    string? PayerEmail,

    [property: JsonPropertyName("card_token_id")]
    [Required(ErrorMessage = "O token do cartão é obrigatório.")]
    string? CardTokenId,

    [property: JsonPropertyName("back_url")]
    [Url(ErrorMessage = "A URL de retorno deve ser uma URL válida.")]
    string? BackUrl,

    [property: JsonPropertyName("reason")]
    [Required(ErrorMessage = "A razão/motivo é obrigatória.")]
    string? Reason
);

/// <summary>
/// Representa os dados necessários para criar uma nova assinatura usando um cartão já salvo.
/// </summary>
public record SubscriptionWithCardRequestDto(
    [property: JsonPropertyName("preapproval_plan_id")]
    [Required(ErrorMessage = "O ID do plano é obrigatório.")]
    string? PreapprovalPlanId,

    [property: JsonPropertyName("card_id")]
    [Required(ErrorMessage = "O ID do cartão é obrigatório.")]
    string? CardId,

    [property: JsonPropertyName("payer")]
    [Required(ErrorMessage = "As informações do pagador são obrigatórias.")]
    PayerRequestDto? Payer
);

/// <summary>
/// Representa a resposta detalhada da API do provedor para uma assinatura.
/// </summary>
public record SubscriptionResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("preapproval_plan_id")] string? PreapprovalPlanId,
    [property: JsonPropertyName("payer_id")] long PayerId,
    [property: JsonPropertyName("payer_email")] string? PayerEmail,
    [property: JsonPropertyName("date_created")] DateTime DateCreated,
    [property: JsonPropertyName("next_invoice_date")] DateTime? NextBillingDate,
    [property: JsonPropertyName("card")] SubscriptionCardDto? Card
);

/// <summary>
/// DTO auxiliar que representa os detalhes do cartão aninhado na resposta da assinatura.
/// </summary>
public record SubscriptionCardDto(
    [property: JsonPropertyName("last_four_digits")] string? LastFourDigits,
    [property: JsonPropertyName("brand")] string? Brand
);

/// <summary>
/// Representa os dados para atualizar o status de uma assinatura existente.
/// </summary>
public record UpdateSubscriptionStatusDto(
    [property: JsonPropertyName("status")]
    [Required(ErrorMessage = "O novo status é obrigatório.")]
    string? Status
);

/// <summary>
/// Representa os dados para atualizar o valor da transação de uma assinatura existente.
/// </summary>
public record UpdateSubscriptionValueDto(
    [property: JsonPropertyName("transaction_amount")]
    [Range(
        typeof(decimal),
        "0.01",
        "1000000.00",
        ErrorMessage = "O valor da transação deve ser positivo."
    )]
    decimal TransactionAmount,

    [property: JsonPropertyName("currency_id")]
    [Required(ErrorMessage = "O ID da moeda é obrigatório.")]
    string CurrencyId = "BRL" // O valor padrão pode ser definido diretamente no construtor do record
);