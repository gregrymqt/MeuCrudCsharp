namespace MeuCrudCsharp.Features.Exceptions
{
    // Específica para quando uma API externa (Mercado Pago, SendGrid) falha.
    public class ExternalApiException : AppServiceException // Herda da base
    {
        public ExternalApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}