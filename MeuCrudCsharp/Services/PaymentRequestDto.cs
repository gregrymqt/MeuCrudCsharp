namespace MeuCrudCsharp.Services
{
    public class PaymentRequestDto
    {
        public string Token { get; set; }
        public int Installments { get; set; }
        public string PaymentMethodId { get; set; }
        public string IssuerId { get; set; }
        public PayerDto Payer { get; set; }
    }
}
