using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class AutoRecurringDto
    {
        [JsonPropertyName("frequency")]
        public int Frequency { get; set; }

        [JsonPropertyName("frequency_type")]
        public string FrequencyType { get; set; } = string.Empty;

        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("currency_id")]
        public string CurrencyId { get; set; } = "BRL";
    }
}
