using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

/// <summary>
/// Service responsável por processar criação de assinatura.
/// Usa o padrão Unit of Work para garantir transações atômicas.
/// </summary>
public class SubscriptionCreateNotificationService(
    ILogger<SubscriptionCreateNotificationService> logger,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    IEmailSenderService emailService,
    IRazorViewToStringRenderer razorViewToStringRenderer,
    IOptions<GeneralSettings> generalSettings)
    : ISubscriptionCreateNotificationService
{
    private readonly GeneralSettings _generalSettings = generalSettings.Value;

    /// <summary>
    /// Verifica e processa a criação de uma assinatura.
    /// </summary>
    /// <param name="externalId">ID externo da assinatura.</param>
    public async Task VerifyAndProcessSubscriptionAsync(string externalId)
    {
        logger.LogInformation(
            "Iniciando processamento de criação de assinatura {ExternalId}.",
            externalId
        );

        try
        {
            // 1. Busca assinatura via Repository (com User e Plan incluídos)
            var subscription = await subscriptionRepository.GetByExternalIdAsync(
                externalId,
                includePlan: true,
                asNoTracking: false // ✅ Precisa rastrear para salvar mudanças
            );

            if (subscription == null)
            {
                throw new ResourceNotFoundException(
                    $"Assinatura com ExternalId {externalId} não encontrada no banco."
                );
            }

            // 2. Verifica idempotência - se já foi processado
            if (subscription.Status != "pending" && subscription.Status != "in_process")
            {
                logger.LogInformation(
                    "Assinatura {ExternalId} já foi processada (Status: {Status}). Ignorando.",
                    externalId,
                    subscription.Status
                );
                return;
            }

            // 3. Atualiza status da assinatura
            subscription.Status = "active"; // ✅ Status correto
            subscriptionRepository.Update(subscription); // ✅ Marca para update

            logger.LogInformation(
                "Assinatura {ExternalId} marcada para atualização com status 'active'.",
                externalId
            );

            // ✅ 4. COMMIT - Salva a mudança no banco (AGORA É SALVO!)
            await unitOfWork.CommitAsync();

            logger.LogInformation(
                "Assinatura {ExternalId} atualizada com sucesso no banco de dados.",
                externalId
            );

            // 5. Envia email APÓS persistência bem-sucedida
            if (subscription is { User: not null, Plan: not null })
            {
                await SendSubscriptionCreatedEmailAsync(subscription);
            }
            else
            {
                logger.LogWarning(
                    "Assinatura {ExternalId} não possui User ou Plan associado. Email não será enviado.",
                    externalId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar criação de assinatura {ExternalId}",
                externalId
            );
            throw; // Rollback automático
        }
    }

    /// <summary>
    /// Envia email de confirmação de criação de assinatura.
    /// </summary>
    private async Task SendSubscriptionCreatedEmailAsync(Models.Subscription subscription)
    {
        if (string.IsNullOrEmpty(subscription.User?.Email))
        {
            logger.LogWarning(
                "Assinatura {SubscriptionId} sem email do usuário. Email não será enviado.",
                subscription.Id
            );
            return;
        }

        var viewModel = new SubscriptionCreateEmailViewModel(
            userName: subscription.User.Name ?? "Cliente",
            planName: subscription.Plan?.Name ?? "Plano",
            subscriptionId: subscription.Id,
            currentPeriodEndDate: subscription.CurrentPeriodEndDate,
            accountUrl: $"{_generalSettings.BaseUrl}/Profile/User/index.cshtml"
        );

        await SendEmailFromTemplateAsync(
            recipientEmail: subscription.User.Email,
            subject: "Sua assinatura foi criada com sucesso!",
            viewPath: "/Pages/EmailTemplates/SubscriptionCreate/index.cshtml",
            model: viewModel
        );
    }

    private async Task SendEmailFromTemplateAsync<TModel>(
        string recipientEmail,
        string subject,
        string viewPath,
        TModel model
    )
    {
        logger.LogInformation(
            "Iniciando montagem de e-mail a partir do template '{ViewPath}' para {RecipientEmail}.",
            viewPath,
            recipientEmail
        );

        try
        {
            var htmlBody = await razorViewToStringRenderer.RenderViewToStringAsync(
                viewPath,
                model
            );

            var plainTextBody =
                $"Assunto: {subject}. Para visualizar esta mensagem, utilize um leitor de e-mail compatível com HTML.";

            await emailService.SendEmailAsync(recipientEmail, subject, htmlBody, plainTextBody);

            logger.LogInformation(
                "E-mail para {RecipientEmail} enviado com sucesso.",
                recipientEmail
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha no processo de montagem e envio de e-mail para {RecipientEmail}.",
                recipientEmail
            );
            throw;
        }
    }
}
