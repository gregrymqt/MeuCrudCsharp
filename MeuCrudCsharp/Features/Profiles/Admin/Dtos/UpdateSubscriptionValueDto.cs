using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class UpdateSubscriptionValueDto
    {
        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("currency_id")]
        public string CurrencyId { get; set; } = "BRL";
    }
}
