using System;

namespace MeuCrudCsharp.Features.Exceptions
{
    /// <summary>
    /// Representa erros que ocorrem quando um recurso específico solicitado não pode ser encontrado.
    /// Esta exceção é tipicamente usada para sinalizar uma condição que deve resultar em uma resposta HTTP 404 Not Found.
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ResourceNotFoundException"/> com uma mensagem de erro especificada.
        /// </summary>
        /// <param name="message">A mensagem que descreve o erro.</param>
        public ResourceNotFoundException(string message)
            : base(message) { }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="ResourceNotFoundException"/> com uma mensagem de erro especificada e uma referência à exceção interna que é a causa desta exceção.
        /// </summary>
        /// <param name="message">A mensagem que descreve o erro.</param>
        /// <param name="innerException">A exceção que é a causa da exceção atual.</param>
        public ResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
