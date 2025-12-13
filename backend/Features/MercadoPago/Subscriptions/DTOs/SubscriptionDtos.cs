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
    [property: JsonPropertyName("reason")] // ✅ ADICIONADO
    string? Reason, 
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
    [property: JsonPropertyName("auto_recurring")]
    AutoRecurringDto? AutoRecurring,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("external_reference")]
    string? ExternalReference
);

/// <summary>
/// Representa a resposta da API do Mercado Pago ao criar ou consultar uma assinatura.
/// </summary>
public record SubscriptionResponseDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("preapproval_plan_id")]
    string? PreapprovalPlanId,
    [property: JsonPropertyName("payer_id")]
    long? PayerId,
    [property: JsonPropertyName("payer_email")]
    string? PayerEmail,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("date_created")]
    DateTime DateCreated,
    [property: JsonPropertyName("last_modified")]
    DateTime LastModified,
    [property: JsonPropertyName("next_payment_date")]
    DateTime? NextPaymentDate,
    [property: JsonPropertyName("auto_recurring")]
    AutoRecurringDto? AutoRecurring,
    [property: JsonPropertyName("payment_method_id")]
    string? PaymentMethodId
);

/// <summary>
/// Representa o bloco de informações de recorrência da assinatura.
/// </summary>
public record AutoRecurringDto(
    [property: JsonPropertyName("frequency")]
    int Frequency,
    [property: JsonPropertyName("frequency_type")]
    string? FrequencyType,
    [property: JsonPropertyName("transaction_amount")]
    decimal TransactionAmount,
    [property: JsonPropertyName("currency_id")]
    string? CurrencyId,
    [property: JsonPropertyName("start_date")]
    DateTime StartDate,
    [property: JsonPropertyName("end_date")]
    DateTime EndDate
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