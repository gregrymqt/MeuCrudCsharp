// Em Features/MercadoPago/Payments/Models/MercadoPagoPaymentDetails.cs

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Dtos
{
    public class MercadoPagoPaymentDetails
    {
        // Usamos JsonPropertyName para mapear o snake_case da API para o PascalCase do C#
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string Status { get; set; }
        // Adicione outros campos que precisar, como "status_detail"
    }
}
