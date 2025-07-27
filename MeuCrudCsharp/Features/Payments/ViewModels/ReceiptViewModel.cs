using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Payments.ViewModels
{
    public class ReceiptViewModel
    {
        [Required]
        [MaxLength(50)]
        public string CompanyName { get; set; }

        [Required]
        [MaxLength(21)]
        public string CompanyCnpj { get; set; }

        [Required]
        [MaxLength(30)]
        public string PaymentId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string CustomerName { get; set; }

        [Required]
        [MaxLength(15)]
        public string CustomerCpf { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(25)]
        public string PaymentMethod { get; set; }

        [Required]
        public int LastFourDigits { get; set; }
    }
}
