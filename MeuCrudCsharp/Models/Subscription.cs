namespace MeuCrudCsharp.Models
{
    public class Subscription
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string SubscriptionId { get; set; } // ID do Mercado Pago
        public string PlanId { get; set; }
        public string Status { get; set; }
        public string PayerEmail { get; set; }
        // Adicione outras colunas se precisar, como Data de Criação, etc.
    }
}
