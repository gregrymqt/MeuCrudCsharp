using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    /// <summary>
    /// Represents the automatic recurring payment details for a subscription plan.
    /// </summary>
    public class AutoRecurringDto
    {
        /// <summary>
        /// The frequency interval for the recurring payment (e.g., 1 for every 1 month).
        /// </summary>
        [JsonPropertyName("frequency")]
        [Range(1, int.MaxValue, ErrorMessage = "Frequency must be a positive number.")]
        public int Frequency { get; set; }

        /// <summary>
        /// The type of frequency (e.g., "months", "years").
        /// </summary>
        [JsonPropertyName("frequency_type")]
        [Required(ErrorMessage = "Frequency type is required.")]
        public string FrequencyType { get; set; }

        /// <summary>
        /// The amount to be charged in each recurring transaction.
        /// </summary>
        [JsonPropertyName("transaction_amount")]
        [Range(
            typeof(decimal),
            "0.01",
            "1000000.00",
            ErrorMessage = "Transaction amount must be a positive value."
        )]
        public decimal TransactionAmount { get; set; }

        /// <summary>
        /// The currency identifier for the transaction amount (e.g., "BRL").
        /// </summary>
        [JsonPropertyName("currency_id")]
        [Required(ErrorMessage = "Currency ID is required.")]
        public string CurrencyId { get; set; } = "BRL";
    }
}
