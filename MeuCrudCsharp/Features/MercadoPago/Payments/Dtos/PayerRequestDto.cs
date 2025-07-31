namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos
{
    public class PayerRequestDto
    {
        public string? Email { get; set; }
        public IdentificationDto? Identification { get; set; }
    }
}
