// Models/Plan.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(IsActive))]
    public class Plan
    {
        [Key]
        public Guid Id { get; set; }

        // ID do plano no provedor de pagamento (ex: Mercado Pago Pre-approval Plan ID)
        [Required]
        [MaxLength(255)]
        public string? ExternalPlanId { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Name { get; set; } // Ex: "Plano Anual", "Plano Mensal"

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TransactionAmount { get; set; }

        [Required]
        [MaxLength(10)]
        public string CurrencyId { get; set; } = "BRL";

        [Required]
        public int Frequency { get; set; } // Ex: 1

        [Required]
        [MaxLength(20)]
        public string? FrequencyType { get; set; } // Ex: "months", "years"

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
