using System;

namespace MeuCrudCsharp.Features.Exceptions
{
    /// <summary>
    /// Representa erros que ocorrem durante a comunicação com uma API externa, como um provedor de pagamento ou serviço de e-mail.
    /// Esta exceção herda de <see cref="AppServiceException"/> e é usada para encapsular falhas de serviços de terceiros.
    /// </summary>
    public class ExternalApiException : AppServiceException
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ExternalApiException"/> com uma mensagem de erro especificada
        /// e uma referência à exceção interna que é a causa desta exceção.
        /// </summary>
        /// <param name="message">A mensagem que descreve o erro, geralmente indicando qual serviço externo falhou.</param>
        /// <param name="innerException">A exceção original lançada pelo cliente da API ou biblioteca de terceiros.</param>
        public ExternalApiException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
