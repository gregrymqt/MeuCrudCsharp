public class GeneralSettings
{
    public const string SectionName = "General";
    public string BaseUrl { get; set; }
}

public class MercadoPagoSettings
{
    public const string SectionName = "MercadoPago";

    public string PublicKey { get; set; }
    public string AccessToken { get; set; }
    public string WebhookSecret { get; set; }
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

public class JwtSettings
{
    public string Key { get; set; }
}

public class GoogleSettings
{
    public const string SectionName = "Google";
    
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}