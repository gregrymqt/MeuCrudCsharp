using System;
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

        public string? LastFourCardDigits { get; set; }

        // --- CONTROLE DE TEMPO DA ASSINATURA ---

        /// <summary>
        /// Data de início do período de cobrança atual.
        /// Na criação, será igual ao CreatedAt. Em renovações, esta data é atualizada.
        /// </summary>
        [Required]
        public DateTime CurrentPeriodStartDate { get; set; }

        /// <summary>
        /// Data final do período de cobrança atual.
        /// É a principal data para verificar se a assinatura está ativa.
        /// (Ex: Se hoje for antes dessa data, a assinatura está ativa).
        /// </summary>
        [Required]
        public DateTime CurrentPeriodEndDate { get; set; }
    }
}