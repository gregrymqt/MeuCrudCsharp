using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(UserId))]
    [Index(nameof(PlanId))] // ALTERAÇÃO: Indexar pela FK interna (int)
    [Index(nameof(ExternalId), IsUnique = true)]
    [Index(nameof(Status))]
    [Index(nameof(PayerEmail))]
    public class Subscription : TransactionBase
    {
        // --- RELACIONAMENTO COM PLAN ---

        // 1. A Chave Estrangeira (FK) real para o banco. É um 'int' para ter a melhor performance
        //    no join com a tabela de Planos.
        [Required]
        public int PlanId { get; set; }

        // 2. A propriedade de navegação que o Entity Framework usa para ligar os objetos.
        [ForeignKey("PlanId")]
        public virtual Plan? Plan { get; set; }

        // 3. O PublicId do plano associado. NÃO é mapeado no banco, mas pode ser útil
        //    para DTOs ou lógica de negócio, para não ter que fazer um join sempre.
        [NotMapped]
        public Guid PlanPublicId { get; set; }

        public string? LastFourCardDigits { get; set; }
    }
}