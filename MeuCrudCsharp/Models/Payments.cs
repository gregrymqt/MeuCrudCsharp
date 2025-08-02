// Models/Payment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(UserId))]
    public class Payments : TransactionBase // Herda da classe base
    {
        // Propriedades herdadas de TransactionBase:
        // - Id
        // - ExternalId (será usado para o PaymentId do MP)
        // - UserId
        // - User
        // - Status
        // - PayerEmail
        // - CreatedAt, UpdatedAt

        // Propriedades específicas de um Pagamento Único
        [Required]
        [MaxLength(20)]
        public string? Method { get; set; }

        [Required]
        public int Installments { get; set; }

        public DateTime? DateApproved { get; set; }

        [Required]
        public int LastFourDigits { get; set; }

        [Required]
        [MaxLength(15)]
        public string? CustomerCpf { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }
    }
}
