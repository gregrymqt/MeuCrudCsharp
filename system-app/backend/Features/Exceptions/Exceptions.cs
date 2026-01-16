namespace MeuCrudCsharp.Features.Exceptions
{
    // Erro genérico de regra de negócio (ex: "Saldo insuficiente")
    public class AppServiceException : Exception
    {
        public AppServiceException(string message)
            : base(message) { }

        public AppServiceException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    // Erro específico de API externa (ex: "Mercado Pago rejeitou")
    // Herda de AppServiceException, então também será tratado como Regra de Negócio (400)
    public class ExternalApiException : AppServiceException
    {
        public ExternalApiException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    // Erro de quando não acha algo no banco (ex: "Cliente não existe")
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException(string message)
            : base(message) { }

        public ResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
