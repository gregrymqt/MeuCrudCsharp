using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

// Namespace unificado para todos os DTOs relacionados a pagamentos do Mercado Pago.
namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

/// <summary>
/// Requisição para criar um pagamento via PIX.
/// </summary>
public record CreatePixPaymentRequest(
    string? Description,
    decimal TransactionAmount,
    PayerRequestDto? Payer
);

/// <summary>
/// Representa os dados de uma requisição de pagamento com cartão de crédito enviada pelo frontend.
/// </summary>
public record CreditCardPaymentRequestDto(
    [Required] string Token,
    [Required] int Installments,
    [Required] string PaymentMethodId,
    [Required] string IssuerId,
    [Required] decimal Amount,
    // Opcionais (pois dependem se é assinatura ou pagamento único)
    string? Plano, // "anual", "mensal"
    string? PlanExternalId, // Guid do plano no seu banco
    [Required] PayerRequestDto Payer
);

/// <summary>
/// Representa os dados de identificação de um pagador (Payer).
/// </summary>
public record IdentificationDto(string? Type, string? Number);

/// <summary>
/// Detalhes do pagamento retornados pela API do Mercado Pago.
/// </summary>
public record MercadoPagoPaymentDetails(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("external_reference")] string ExternalReference,
    [property: JsonPropertyName("payer")] PayerRequestDto Payer
);

/// <summary>
/// Representa os dados do pagador (payer) em uma requisição de pagamento.
/// </summary>
public record PayerRequestDto(
    string? Email,
    string? FirstName,
    string? LastName,
    IdentificationDto? Identification
);

/// <summary>
/// Representa a resposta simplificada de uma operação de pagamento.
/// Contém as informações essenciais para o frontend e para o armazenamento local.
/// </summary>
public record PaymentResponseDto(
    string? Status,
    long? Id,
    string? PaymentTypeId,
    string? Message,
    string? QrCodeBase64,
    string? QrCode
);

public record CreatePreferenceDto(
    decimal Amount,
    string Title, // Ex: "Curso de Psicologia"
    string Description
);
