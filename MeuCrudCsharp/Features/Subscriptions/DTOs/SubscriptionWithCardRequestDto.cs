using System.Text.Json.Serialization;

// Este DTO representa o corpo da requisição para criar a assinatura
public class SubscriptionWithCardRequestDto
{
    [JsonPropertyName("preapproval_plan_id")]
    public string PreapprovalPlanId { get; set; }

    [JsonPropertyName("card_id")]
    public string CardId { get; set; }

    [JsonPropertyName("payer")] // A propriedade agora é um objeto 'payer'
    public PayerRequestDto Payer { get; set; }
}

// Este é o objeto aninhado para o pagador
public class PayerRequestDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; }
}
