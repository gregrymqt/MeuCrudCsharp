namespace MeuCrudCsharp.Features.Exceptions
{
    public class AppServiceException : Exception
    {
        public AppServiceException(string message)
            : base(message) { }

        public AppServiceException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
