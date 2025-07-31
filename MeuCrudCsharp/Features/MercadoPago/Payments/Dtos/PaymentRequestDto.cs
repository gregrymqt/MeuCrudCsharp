using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

public class PaymentRequestDto
{
    public string Token { get; set; }
    public int Installments { get; set; }
    public string PaymentMethodId { get; set; }
    public string IssuerId { get; set; }
    public PayerRequestDto Payer { get; set; }
    public decimal Amount { get; set; } // Adicione esta propriedade
    public string? Plano { get; set; }
    public string? PreapprovalPlanId { get; set; }
}
