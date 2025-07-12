namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos
{
    public class PaymentResponseDto
    {
        public string Status { get; set; }
        public long? Id { get; set; }
        public string PaymentTypeId { get; set; }
    }
}
