namespace MeuCrudCsharp.Features.Plans.DTOs
{
    public class PlanDto
    {
        public string Name { get; set; } // Ex: "Plano Anual"
        public string Slug { get; set; } // Ex: "anual"
        public string PriceDisplay { get; set; } // Ex: "R$ 41,58/mês"
        public string BillingInfo { get; set; } // Ex: "Cobrado R$ 499,00 anualmente"
        public List<string> Features { get; set; }
        public bool IsRecommended { get; set; }
    }
}
