namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;

public class CreatePixPaymentRequest
{
    public string? Description { get; set; }
    
    public decimal TransactionAmount { get; set; }
    
    public PayerRequestDto? Payer { get; set; }
}