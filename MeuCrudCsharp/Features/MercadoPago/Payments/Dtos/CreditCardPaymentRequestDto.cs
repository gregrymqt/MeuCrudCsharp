using System.ComponentModel.DataAnnotations;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

/// <summary>
/// Representa os dados de uma requisição de pagamento enviada pelo frontend.
/// Contém todas as informações necessárias para processar um pagamento único ou criar uma assinatura.
/// </summary>
public class CreditCardPaymentRequestDto
{
    /// <summary>
    /// O token do cartão de crédito gerado pelo frontend (Payment Brick). Obrigatório.
    /// </summary>
    [Required(ErrorMessage = "O token do cartão é obrigatório.")]
    public string? Token { get; set; }

    /// <summary>
    /// O número de parcelas para o pagamento. Obrigatório.
    /// </summary>
    [Required(ErrorMessage = "O número de parcelas é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = "O número de parcelas deve ser no mínimo 1.")]
    public int Installments { get; set; }

    /// <summary>
    /// O identificador do método de pagamento (ex: "visa", "master"). Obrigatório.
    /// </summary>
    [Required(ErrorMessage = "O método de pagamento é obrigatório.")]
    public string? PaymentMethodId { get; set; }

    /// <summary>
    /// O identificador do emissor do cartão.
    /// </summary>
    public string? IssuerId { get; set; }

    /// <summary>
    /// Os dados do pagador (e-mail e documento). Obrigatório.
    /// </summary>
    [Required(ErrorMessage = "Os dados do pagador são obrigatórios.")]
    public PayerRequestDto? Payer { get; set; }

    /// <summary>
    /// O valor total da transação. Obrigatório.
    /// </summary>
    [Required(ErrorMessage = "O valor do pagamento é obrigatório.")]
    [Range(
        typeof(decimal),
        "0.01",
        "1000000.00",
        ErrorMessage = "O valor do pagamento deve ser positivo."
    )]
    public decimal Amount { get; set; }

    /// <summary>
    /// O nome do plano de assinatura, se aplicável.
    /// </summary>
    public string? Plano { get; set; }

    /// <summary>
    /// O ID do plano de pré-aprovação para criar uma assinatura. Se fornecido, indica uma transação recorrente.
    /// </summary>
    public string? PreapprovalPlanId { get; set; }
}
