// Models/TransactionBase.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    public abstract class TransactionBase
    {
        [Key]
        public Guid Id { get; set; }

        // ID externo (do Mercado Pago, por exemplo)
        [Required]
        public string? ExternalId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [Required]
        [MaxLength(20)]
        public string? Status { get; set; }

        // Adicionando PayerEmail como campo comum
        [Required]
        [MaxLength(255)]
        public string? PayerEmail { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        protected TransactionBase()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
