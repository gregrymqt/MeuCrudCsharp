using System;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    /// <summary>
    /// Define o contrato para um serviço responsável por processar notificações de pagamento.
    /// </summary>
    public interface INotificationPayment
    {
        /// <summary>
        /// Verifica o status de um pagamento com o provedor externo e processa a notificação,
        /// atualizando o estado da transação no sistema local.
        /// </summary>
        /// <param name="internalPaymentId">O ID do pagamento gerado pelo provedor externo (ex: Mercado Pago).</param>
        /// <returns>Uma <see cref="Task"/> que representa a operação de verificação e processamento assíncrona.</returns>
        Task VerifyAndProcessNotificationAsync(string internalPaymentId);
    }
}
