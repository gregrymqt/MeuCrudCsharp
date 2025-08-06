// Models/Subscription.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(UserId))]
    [Index(nameof(PlanId))]
    [Index(nameof(ExternalId), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(PayerEmail))]
    [Index(nameof(CreatedAt))]
    public class Subscription : TransactionBase // Herda da classe base
    {
        // Propriedades herdadas de TransactionBase:
        // - Id
        // - ExternalId (será usado para o SubscriptionId do MP)
        // - UserId
        // - User
        // - Status
        // - PayerEmail
        // - CreatedAt, UpdatedAt

        // Propriedades específicas de uma Assinatura
        [Required]
        public Guid PlanId { get; set; }

        [ForeignKey("PlanId")]
        public virtual Plan? Plan { get; set; }
        public string? LastFourCardDigits { get; set; } // Últimos 4 dígitos do cartão usado na assinatura

    }
}
