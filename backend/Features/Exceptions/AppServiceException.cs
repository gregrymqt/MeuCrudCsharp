using System;

namespace MeuCrudCsharp.Features.Exceptions
{
    /// <summary>
    /// Representa erros que ocorrem durante a execução da lógica de negócios na camada de serviço da aplicação.
    /// Esta exceção é usada para encapsular erros específicos do domínio ou exceções de infraestrutura,
    /// fornecendo uma mensagem clara para as camadas superiores.
    /// </summary>
    public class AppServiceException : Exception
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="AppServiceException"/> com uma mensagem de erro especificada.
        /// </summary>
        /// <param name="message">A mensagem que descreve o erro.</param>
        public AppServiceException(string message)
            : base(message) { }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="AppServiceException"/> com uma mensagem de erro especificada
        /// e uma referência à exceção interna que é a causa desta exceção.
        /// </summary>
        /// <param name="message">A mensagem que descreve o erro.</param>
        /// <param name="innerException">A exceção que é a causa da exceção atual, ou uma referência nula se nenhuma exceção interna for especificada.</param>
        public AppServiceException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
