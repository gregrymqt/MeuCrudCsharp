using System.Text.Json;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

/// <summary>
/// Service responsável por processar notificações de pagamento do Mercado Pago.
/// Usa o padrão Unit of Work para garantir transações atômicas.
/// Coordena Payment, Subscription e envio de emails.
/// </summary>
public class NotificationPaymentService(
    IPaymentRepository paymentRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    IEmailSenderService emailSender,
    IRazorViewToStringRenderer razorRenderer,
    ILogger<NotificationPaymentService> logger,
    IMercadoPagoPaymentService mercadoPagoService,
    IRefundNotification refundNotification,
    ISubscriptionService subscriptionService)
    : INotificationPayment
{

    /// <summary>
    /// Verifica e processa uma notificação de pagamento do Mercado Pago.
    /// </summary>
    /// <param name="internalPaymentId">ID interno do pagamento.</param>
    public async Task VerifyAndProcessNotificationAsync(string internalPaymentId)
    {
        logger.LogInformation(
            "Iniciando processamento de notificação para PaymentId: {PaymentId}",
            internalPaymentId
        );

        try
        {
            // 1. Busca o pagamento via Repository (com User incluído)
            var localPayment = await paymentRepository.GetByIdWithUserAsync(internalPaymentId);
            
            if (localPayment == null)
                throw new ResourceNotFoundException(
                    $"Pagamento com ID {internalPaymentId} não foi encontrado."
                );

            var user = localPayment.User;
            if (user == null)
                throw new ResourceNotFoundException(
                    $"Usuário associado ao pagamento {internalPaymentId} não foi encontrado."
                );

            // 2. Verifica idempotência - se já foi processado, não faz nada
            if (localPayment.Status != "pending" && localPayment.Status != "in_process")
            {
                logger.LogInformation(
                    "Pagamento {PaymentId} já foi processado (Status: {Status}). Ignorando notificação.",
                    internalPaymentId,
                    localPayment.Status
                );
                return;
            }

            // 3. Busca o status mais recente do pagamento no Mercado Pago
            if (string.IsNullOrEmpty(localPayment.ExternalId))
            {
                throw new InvalidOperationException(
                    $"Pagamento {internalPaymentId} não possui ExternalId."
                );
            }

            var externPayment = await mercadoPagoService.GetPaymentStatusAsync(localPayment.ExternalId);
            
            if (externPayment == null)
            {
                logger.LogWarning(
                    "Não foi possível obter detalhes do pagamento externo {ExternalId}",
                    localPayment.ExternalId
                );
                throw new Exception(
                    $"Falha ao obter detalhes do pagamento {localPayment.ExternalId ?? "desconhecido"} do Mercado Pago."
                );
            }

            // 4. Processa baseado no status retornado pelo MP
            await ProcessPaymentStatusAsync(localPayment, externPayment, user);

            // ✅ 5. COMMIT - Salva todas as mudanças atomicamente
            await unitOfWork.CommitAsync();

            logger.LogInformation(
                "Pagamento {PaymentId} processado com sucesso. Novo status: {Status}",
                internalPaymentId,
                localPayment.Status
            );

            // 6. Envia email APÓS persistência bem-sucedida
            await SendEmailBasedOnStatusAsync(localPayment.Status, user, internalPaymentId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar notificação para PaymentId: {PaymentId}",
                internalPaymentId
            );
            throw; // Rollback automático
        }
    }

    /// <summary>
    /// Processa o status do pagamento retornado pelo Mercado Pago.
    /// </summary>
    private async Task ProcessPaymentStatusAsync(
        Models.Payments localPayment,
        dynamic externPayment,
        Users user)
    {
        switch (externPayment.Status)
        {
            case "approved":
                await ProcessApprovedPaymentAsync(localPayment, externPayment, user);
                break;

            case "rejected":
            case "cancelled":
                await ProcessRejectedPaymentAsync(localPayment, externPayment);
                break;

            case "refunded":
                await ProcessRefundedPaymentAsync(localPayment);
                break;

            default:
                logger.LogWarning(
                    "Status de pagamento não tratado recebido do Mercado Pago: {Status}",
                    (string)externPayment.Status
                );
                break;
        }
    }

    /// <summary>
    /// Processa pagamento aprovado - atualiza status e cria assinatura se necessário.
    /// </summary>
    private async Task ProcessApprovedPaymentAsync(
        Models.Payments localPayment,
        dynamic externPayment,
        Users user)
    {
        // Verifica se a assinatura JÁ EXISTE
        if (string.IsNullOrEmpty(localPayment.SubscriptionId))
        {
            // SE NÃO EXISTE, é um pagamento único (PIX ou Cartão) - CRIAR A ASSINATURA
            logger.LogInformation(
                "Pagamento {PaymentId} aprovado. Nenhuma assinatura encontrada, criando uma nova...",
                localPayment.Id
            );

            // Extrai os metadados que foram salvos na criação do pagamento
            var metadata = JsonSerializer.Deserialize<PaymentMetadata>(
                externPayment.ExternalReference
            );
            
            if (metadata == null || metadata.PlanPublicId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Metadados (ExternalReference) inválidos ou ausentes no pagamento {externPayment.Id}. Não é possível criar a assinatura."
                );
            }

            // Chama o serviço especializado para criar a assinatura no banco
            if (externPayment.Payer.Email != null)
            {
                await subscriptionService.ActivateSubscriptionFromSinglePaymentAsync(
                    user.Id,
                    metadata.PlanPublicId,
                    externPayment.Id.ToString(),
                    externPayment.Payer.Email,
                    localPayment.LastFourDigits
                );
            }

            logger.LogInformation(
                "Assinatura de pagamento único criada com sucesso para o usuário {UserId}.",
                user.Id
            );
        }
        else
        {
            // SE JÁ EXISTE, é um pagamento de uma assinatura recorrente - ATUALIZAR STATUS
            logger.LogInformation(
                "Pagamento {PaymentId} aprovado. Atualizando status da assinatura existente {SubscriptionId}.",
                localPayment.Id,
                localPayment.SubscriptionId
            );

            var subscription = await subscriptionRepository.GetByIdAsync(localPayment.SubscriptionId);
            if (subscription != null)
            {
                subscription.Status = "active";
                subscriptionRepository.Update(subscription); // ✅ Marca para update
            }
        }

        // Atualiza o status do pagamento local para 'approved'
        localPayment.Status = "approved";
        paymentRepository.Update(localPayment); // ✅ Marca para update
    }

    /// <summary>
    /// Processa pagamento rejeitado ou cancelado.
    /// </summary>
    private async Task ProcessRejectedPaymentAsync(Models.Payments localPayment, dynamic externPayment)
    {
        localPayment.Status = externPayment.Status;
        paymentRepository.Update(localPayment); // ✅ Marca para update

        if (!string.IsNullOrEmpty(localPayment.SubscriptionId))
        {
            var subscription = await subscriptionRepository.GetByIdAsync(localPayment.SubscriptionId);
            if (subscription != null)
            {
                subscription.Status = externPayment.Status;
                subscriptionRepository.Update(subscription); // ✅ Marca para update
            }
        }
    }

    /// <summary>
    /// Processa pagamento reembolsado.
    /// </summary>
    private async Task ProcessRefundedPaymentAsync(Models.Payments localPayment)
    {
        localPayment.Status = "refunded";
        paymentRepository.Update(localPayment); // ✅ Marca para update

        if (!string.IsNullOrEmpty(localPayment.SubscriptionId))
        {
            var subscription = await subscriptionRepository.GetByIdAsync(localPayment.SubscriptionId);
            if (subscription != null)
            {
                subscription.Status = "refunded";
                subscriptionRepository.Update(subscription); // ✅ Marca para update
            }
        }

        // Envia notificação SignalR de reembolso
        await refundNotification.SendRefundStatusUpdate(
            localPayment.UserId,
            "completed",
            "Seu reembolso foi processado com sucesso!"
        );
    }

    /// <summary>
    /// Envia email baseado no status do pagamento.
    /// </summary>
    private async Task SendEmailBasedOnStatusAsync(string status, Users user, string paymentId)
    {
        switch (status)
        {
            case "approved":
                await SendConfirmationEmailAsync(user, paymentId);
                break;

            case "rejected":
            case "cancelled":
                await SendRejectionEmailAsync(user, paymentId);
                break;

            case "refunded":
                await SendRefundConfirmationEmailAsync(user, paymentId);
                break;
        }
    }

    private async Task SendPaymentEmailNotificationAsync(
        Users user,
        string paymentId,
        string subject,
        string viewPath,
        object viewModel,
        string logContext
    )
    {
        try
        {
            var htmlBody = await razorRenderer.RenderViewToStringAsync(viewPath, viewModel);
            var plainTextBody = $"Olá, {user.Name ?? "Cliente"}! Novidades sobre seu pagamento {paymentId}.";

            if (!string.IsNullOrEmpty(user.Email))
            {
                await emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                logger.LogInformation(
                    "E-mail de {LogContext} enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                    logContext,
                    user.Email,
                    paymentId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha ao enviar e-mail de {LogContext} para {UserEmail} (PaymentId: {PaymentId}).",
                logContext,
                user.Email,
                paymentId
            );
            throw new ExternalApiException(
                $"Falha ao renderizar ou enviar o e-mail de {logContext}.",
                ex
            );
        }
    }

    private async Task SendConfirmationEmailAsync(Users user, string paymentId)
    {
        var viewModel = new ConfirmationEmailViewModel
        {
            UserName = user.Name ?? "Cliente",
            PaymentId = paymentId,
        };
        await SendPaymentEmailNotificationAsync(
            user,
            paymentId,
            "Seu pagamento foi aprovado! 🎉",
            "~/Pages/EmailTemplates/Confirmation/Email.cshtml",
            viewModel,
            "Confirmação"
        );
    }

    private async Task SendRejectionEmailAsync(Users user, string paymentId)
    {
        var viewModel = new RejectionEmailViewModel()
        {
            UserName = user.Name ?? "Cliente",
            PaymentId = paymentId,
        };
        await SendPaymentEmailNotificationAsync(
            user,
            paymentId,
            "Atenção: Ocorreu um problema com seu pagamento",
            "~/Pages/EmailTemplates/Rejection/Email.cshtml",
            viewModel,
            "Rejeição"
        );
    }

    private async Task SendRefundConfirmationEmailAsync(Users user, string paymentId)
    {
        var viewModel = new ConfirmationEmailViewModel
        {
            UserName = user.Name ?? "Cliente",
            PaymentId = paymentId,
        };
        await SendPaymentEmailNotificationAsync(
            user,
            paymentId,
            "Seu Reembolso foi aprovado! 🎉",
            "~/Pages/EmailTemplates/Refund/Email.cshtml",
            viewModel,
            "Confirmação de Reembolso"
        );
    }
}
