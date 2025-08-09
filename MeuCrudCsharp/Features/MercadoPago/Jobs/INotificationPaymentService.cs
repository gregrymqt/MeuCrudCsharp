using System;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    /// <summary>
    /// Define o contrato para um serviço responsável por processar notificações de pagamento.
    /// </summary>
    public interface INotificationPaymentService
    {
        /// <summary>
        /// Verifica o status de um pagamento com o provedor externo e processa a notificação,
        /// atualizando o estado da transação no sistema local.
        /// </summary>
        /// <param name="userId">O ID do usuário no sistema local associado ao pagamento.</param>
        /// <param name="paymentID">O ID do pagamento gerado pelo provedor externo (ex: Mercado Pago).</param>
        /// <returns>Uma <see cref="Task"/> que representa a operação de verificação e processamento assíncrona.</returns>
        Task VerifyAndProcessNotificationAsync(Guid userId, string paymentID);
    }
}
