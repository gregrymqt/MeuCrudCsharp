
public class RedirectSettings
{
    public string Url { get; set; }
}

public class PaymentSettings
{
    public string NotificationUrl { get; set; }
}

// ADICIONE ESTAS NOVAS CLASSES ABAIXO

public class MercadoPagoSettings
{
    public const string SectionName = "MERCADOPAGO";

    public string PublicKey { get; set; }
    public string AccessToken { get; set; } // Adicionado para o AccessToken
    public string WebhookSecret { get; set; } // Adicionado para o segredo do Webhook
    public string Defaultdescription { get; set; }
    public PlanConfiguration Plans { get; set; }
}

public class PlanConfiguration
{
    public PlanDetail Mensal { get; set; }
    public PlanDetail Anual { get; set; }
}

public class PlanDetail
{
    public string Id { get; set; }
    public decimal Price { get; set; }
}

public class SendGridSettings
{
    public const string SectionName = "SendGrid";
    public string ApiKey { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
}