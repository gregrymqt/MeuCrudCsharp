// Em Models/Payment_User.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // Este using ainda é essencial!

namespace MeuCrudCsharp.Models
{
    // CORREÇÃO APLICADA AQUI:
    // O atributo [Index] é colocado sobre a classe, e especificamos
    // o nome da propriedade que queremos indexar dentro dele.
    [Index(nameof(UserId))]
    public class Payment_User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string PaymentId { get; set; }

        // A propriedade da chave estrangeira. O índice agora é definido na classe acima.
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

        [Required]
        [MaxLength(20)]
        public string Method { get; set; }

        [Required]
        public int Installments { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        public Payment_User()
        {
            Id = Guid.NewGuid();
        }
    }
}