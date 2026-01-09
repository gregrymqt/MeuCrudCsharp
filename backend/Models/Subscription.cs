using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(UserId))]
    [Index(nameof(PlanId))]
    [Index(nameof(ExternalId), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(PayerEmail))]
    [Index(nameof(CurrentPeriodEndDate))] // <-- NOVO: Indexar a data de expiração é ótimo para performance
    public class Subscription : TransactionBase
    {
        // --- RELACIONAMENTO COM PLAN ---
        [Required]
        public int PlanId { get; set; }

        [ForeignKey("PlanId")]
        public virtual Plan? Plan { get; set; }

        [NotMapped]
        public Guid PlanPublicId { get; set; }

        [Required]
        public string? LastFourCardDigits { get; set; }

        [Required]
        public string? PayerMpId { get; set; }

        [Required]
        public int CurrentAmount { get; set; }

        [Required]
        public DateTime CurrentPeriodStartDate { get; set; }

        [Required]
        public DateTime CurrentPeriodEndDate { get; set; }

        [Required]
        public string? PaymentMethodId { get; set; }

        public string? CardTokenId { get; set; }
    }
}
