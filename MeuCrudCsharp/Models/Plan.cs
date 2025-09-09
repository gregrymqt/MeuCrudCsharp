using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    // Este modelo já está perfeito e serve como nosso guia.
    // Nenhuma alteração necessária.
    [Index(nameof(PublicId), IsUnique = true)]
    [Index(nameof(IsActive))]
    public class Plan
    {
        [Key] // Chave primária interna (int) -> Ótima performance
        public int Id { get; set; }

        // Identificador público (Guid) -> Ótima segurança
        [Required]
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string ExternalPlanId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TransactionAmount { get; set; }

        [Required]
        [MaxLength(10)]
        public string CurrencyId { get; set; } = "BRL";

        [Required]
        public int Frequency { get; set; }

        [Required]
        [MaxLength(20)]
        public string FrequencyType { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}