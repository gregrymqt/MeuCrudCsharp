using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces; // Ajuste conforme seu namespace
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces; // Ajuste conforme seu namespace
using MeuCrudCsharp.Features.Shared.Work; // Ajuste conforme seu namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Services
{
    public class RefundService(
        IPaymentRepository paymentRepository,
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClient,
        ILogger<RefundService> logger)
        : MercadoPagoServiceBase(httpClient, logger), IRefundService
    {
        public async Task RequestRefundAsync(long paymentId)
        {
            var externalIdStr = paymentId.ToString();

            // 1. Busca usando o novo método do Repository que traz a Assinatura
            var payment = await paymentRepository.GetByExternalIdWithSubscriptionAsync(
                externalIdStr
            );

            // 2. Validações
            if (payment == null)
            {
                throw new ResourceNotFoundException(
                    $"Pagamento {paymentId} não encontrado no sistema."
                );
            }

            if (payment.Status != "aprovada" && payment.Status != "approved")
            {
                throw new AppServiceException(
                    "Apenas pagamentos aprovados podem ser reembolsados."
                );
            }

            if (payment.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                throw new AppServiceException(
                    "O prazo de 7 dias para solicitação de reembolso expirou."
                );
            }

            try
            {
                // 3. Chama a API do Mercado Pago
                await RefundPaymentOnMercadoPagoAsync(payment.ExternalId);

                // 4. Atualiza o status via Repository
                payment.Status = "refunded";
                payment.UpdatedAt = DateTime.UtcNow;

                // Marca o objeto Payment como modificado no contexto
                paymentRepository.Update(payment);

                // Se houver assinatura vinculada, atualizamos via Repository dela
                if (payment.Subscription != null)
                {
                    payment.Subscription.Status = "refund_pending";
                    payment.Subscription.UpdatedAt = DateTime.UtcNow;

                    // Marca o objeto Subscription como modificado
                    subscriptionRepository.Update(payment.Subscription);
                }

                // 5. Persiste tudo de uma vez (Atomicidade)
                await unitOfWork.CommitAsync();

                logger.LogInformation(
                    "Reembolso solicitado com sucesso para o pagamento {ExternalId}.",
                    payment.ExternalId
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Falha ao processar reembolso no Mercado Pago para o pagamento {ExternalId}.",
                    payment.ExternalId
                );
                throw;
            }
        }

        private async Task RefundPaymentOnMercadoPagoAsync(
            string externalPaymentId,
            decimal? amount = null
        )
        {
            logger.LogInformation(
                "Iniciando request de reembolso MP para: {PaymentId}",
                externalPaymentId
            );

            var endpoint = $"/v1/payments/{externalPaymentId}/refunds";
            var payload = new { amount = amount };

            await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        }
    }
}
