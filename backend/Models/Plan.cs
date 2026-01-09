// Models/Plan.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// Enums/PlanFrequencyType.cs
// Este enum representa os valores que a API do Mercado Pago aceita para a frequência.

public enum PlanFrequencyType
{
    Days, // Corresponde a "days" na API
    Months, // Corresponde a "months" na API
}

namespace MeuCrudCsharp.Models
{
    [Index(nameof(PublicId), IsUnique = true)]
    [Index(nameof(IsActive))]
    public class Plan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid PublicId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ID do plano correspondente no Mercado Pago.
        /// Essencial para criar assinaturas vinculadas a ele.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string ExternalPlanId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Valor da transação/cobrança. Mapeia para "transaction_amount" na API.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal TransactionAmount { get; set; }

        /// <summary>
        /// Moeda da transação. Mapeia para "currency_id" na API.
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string CurrencyId { get; set; } = "BRL";

        // --- CAMPOS DE FREQUÊNCIA REFINADOS ---

        /// <summary>
        /// O intervalo da recorrência. Ex: 1, 3, 6.
        /// Mapeia para o campo "frequency" na API do Mercado Pago.
        /// </summary>
        [Required]
        public int FrequencyInterval { get; set; }

        /// <summary>
        /// A unidade de tempo da recorrência (Dias ou Meses).
        /// Mapeia para o campo "frequency_type" na API do Mercado Pago.
        /// </summary>
        [Required]
        public PlanFrequencyType FrequencyType { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;
    }
}
