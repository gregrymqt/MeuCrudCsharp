using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.MercadoPago.ViewModels
{
    /// <summary>
    /// Representa o modelo de dados para a geração de um recibo de pagamento.
    /// Este ViewModel é usado para popular templates de recibo, como views Razor para HTML ou PDF.
    /// </summary>
    public class ReceiptViewModel
    {
        /// <summary>
        /// O nome da empresa que emitiu o recibo.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string? CompanyName { get; set; }

        /// <summary>
        /// O CNPJ da empresa que emitiu o recibo.
        /// </summary>
        [Required]
        [MaxLength(21)]
        public string? CompanyCnpj { get; set; }

        /// <summary>
        /// O identificador único do pagamento.
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string? PaymentId { get; set; }

        /// <summary>
        /// A data e hora em que o pagamento foi realizado.
        /// </summary>
        [Required]
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// O nome completo do cliente que realizou o pagamento.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string? CustomerName { get; set; }

        /// <summary>
        /// O CPF do cliente que realizou o pagamento.
        /// </summary>
        [Required]
        [MaxLength(15)]
        public string? CustomerCpf { get; set; }

        /// <summary>
        /// O valor total do pagamento.
        /// </summary>
        [Required]
        public decimal Amount { get; set; }

        /// <summary>
        /// O método de pagamento utilizado (ex: "Cartão de Crédito", "PIX").
        /// </summary>
        [Required]
        [MaxLength(25)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Os últimos quatro dígitos do cartão de crédito, se aplicável.
        /// </summary>
        [Required]
        public int LastFourDigits { get; set; }
    }
}
