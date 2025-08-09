using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Subscriptions.DTOs
{
    /// <summary>
    /// Represents the data required to update the transaction value of an existing subscription.
    /// </summary>
    public class UpdateSubscriptionValueDto
    {
        /// <summary>
        /// The new amount to be charged in each recurring transaction.
        /// </summary>
        [JsonPropertyName("transaction_amount")]
        [Range(
            typeof(decimal),
            "0.01",
            "1000000.00",
            ErrorMessage = "The transaction amount must be a positive value."
        )]
        public decimal TransactionAmount { get; set; }

        /// <summary>
        /// The currency identifier for the new transaction amount (e.g., "BRL").
        /// </summary>
        [JsonPropertyName("currency_id")]
        [Required(ErrorMessage = "The currency ID is required.")]
        public string CurrencyId { get; set; } = "BRL";
    }
}
