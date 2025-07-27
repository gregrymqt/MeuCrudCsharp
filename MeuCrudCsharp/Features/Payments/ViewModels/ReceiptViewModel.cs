namespace MeuCrudCsharp.Features.Payments.ViewModels
{
    public class ReceiptViewModel
    {
        public string CompanyName { get; set; }

        public string CompanyCnpj { get; set; }

        public string PaymentId { get; set; }

        public System.DateTime PaymentDate { get; set; }

        public string CustomerName { get; set; }

        public string CustomerCpf { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; }
    }
}
