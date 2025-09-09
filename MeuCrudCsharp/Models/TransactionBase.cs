using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    public abstract class TransactionBase
    {
        // A chave primária já é um Guid em formato de string, o que é seguro para expor.
        // Neste caso, ele serve tanto como PK quanto como identificador público.
        [Key]
        public string Id { get; set; }

        // ID externo (do Mercado Pago, por exemplo)
        [Required]
        public string? ExternalId { get; set; }

        // A FK para o usuário já é uma string (padrão do Identity), então está correto.
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [Required]
        [MaxLength(20)]
        public string? Status { get; set; }

        [Required]
        [MaxLength(255)]
        public string? PayerEmail { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? PaymentId { get; set; }

        protected TransactionBase()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
