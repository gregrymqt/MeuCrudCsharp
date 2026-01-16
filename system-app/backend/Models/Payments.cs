using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(UserId))]
    [Index(nameof(SubscriptionId))] // A FK já é um Guid (string), então está ok.
    [Index(nameof(ExternalId), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(PayerEmail))]
    public class Payments : TransactionBase
    {
        [Required]
        [MaxLength(20)]
        public string? Method { get; set; }

        [Required]
        public int Installments { get; set; }

        public DateTime? DateApproved { get; set; }

        [Required]
        public string? LastFourDigits { get; set; }

        [Required]
        [MaxLength(15)]
        public string? CustomerCpf { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public string SubscriptionId { get; set; } = null!;

        [ForeignKey("SubscriptionId")]
        public virtual Subscription? Subscription { get; set; }
    }
}
