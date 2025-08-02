// Models/Subscription.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(UserId))]
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
    }
}
