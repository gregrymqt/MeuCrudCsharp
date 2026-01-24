using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

/// <summary>
/// Service responsável por processar notificações de atualização de planos do Mercado Pago.
/// Usa o padrão Unit of Work para garantir transações atômicas.
/// </summary>
public class PlanUpdateNotificationService(
    IPlanRepository planRepository,
    IUnitOfWork unitOfWork,
    IMercadoPagoPlanService mercadoPagoApiService,
    IRazorViewToStringRenderer razorViewToStringRenderer,
    IEmailSenderService emailSenderService,
    IOptions<GeneralSettings> generalSettings,
    ILogger<PlanUpdateNotificationService> logger,
    IUserContext userContext)
    : IPlanUpdateNotificationService
{
    private readonly GeneralSettings _generalSettings = generalSettings.Value;

    public async Task VerifyAndProcessPlanUpdate(string externalId)
    {
        logger.LogInformation(
            "Iniciando verificação e processamento para o plano com ExternalId: {ExternalId}",
            externalId
        );

        try
        {
            // 1. Busca o plano na API do Mercado Pago
            var mpPlan = await mercadoPagoApiService.GetPlanByExternalIdAsync(externalId);
            if (mpPlan?.Id is null || mpPlan.AutoRecurring is null)
            {
                logger.LogWarning(
                    "Não foi possível obter os detalhes do plano da API do Mercado Pago para o ExternalId: {ExternalId}",
                    externalId
                );
                return;
            }

            // 2. Busca o plano no banco de dados local via Repository
            var localPlan = await planRepository.GetActiveByExternalIdAsync(externalId);

            if (localPlan is null)
            {
                logger.LogWarning(
                    "Plano com ExternalId {ExternalId} encontrado na API do MP, mas não no banco de dados local.",
                    externalId
                );
                return;
            }

            var needsUpdate = false;
            var changes = new List<string>();

            var statusMapping = new Dictionary<string, bool>
            {
                { "active", true },
                { "cancelled", false },
            };

            // 3. Compara os valores e detecta mudanças
            if (localPlan.IsActive != statusMapping[mpPlan.Status!])
            {
                logger.LogInformation(
                    "Diferença de Status detectada para o plano {ExternalId}. Local: '{LocalStatus}', MP: '{MpStatus}'. Atualizando.",
                    externalId,
                    localPlan.IsActive,
                    mpPlan.Status
                );
                localPlan.IsActive = statusMapping[mpPlan.Status!];
                changes.Add($"Status alterado de '{localPlan.IsActive}' para '{mpPlan.Status}'.");
                needsUpdate = true;
            }

            if (localPlan.TransactionAmount != mpPlan.AutoRecurring.TransactionAmount)
            {
                logger.LogInformation(
                    "Diferença de TransactionAmount detectada para o plano {ExternalId}. Local: '{LocalAmount}', MP: '{MpAmount}'. Atualizando.",
                    externalId,
                    localPlan.TransactionAmount,
                    mpPlan.AutoRecurring.TransactionAmount
                );
                localPlan.TransactionAmount = mpPlan.AutoRecurring.TransactionAmount;
                changes.Add(
                    $"Valor da transação alterado de '{localPlan.TransactionAmount:C}' para '{mpPlan.AutoRecurring.TransactionAmount:C}'."
                );
                needsUpdate = true;
            }

            if (localPlan.FrequencyInterval != mpPlan.AutoRecurring.Frequency)
            {
                logger.LogInformation(
                    "Diferença de FrequencyInterval detectada para o plano {ExternalId}. Local: '{LocalFrequency}', MP: '{MpFrequency}'. Atualizando.",
                    externalId,
                    localPlan.FrequencyInterval,
                    mpPlan.AutoRecurring.Frequency
                );
                localPlan.FrequencyInterval = mpPlan.AutoRecurring.Frequency;
                changes.Add(
                    $"Frequência alterada de '{localPlan.FrequencyInterval}' para '{mpPlan.AutoRecurring.Frequency}'."
                );
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                // 4. Marca plano para atualização
                planRepository.Update(localPlan);

                // ✅ 5. COMMIT - Salva as mudanças no banco (AGORA AS MUDANÇAS SÃO SALVAS!)
                await unitOfWork.CommitAsync();

                logger.LogInformation(
                    "Plano {ExternalId} foi atualizado no banco de dados para refletir os dados do Mercado Pago.",
                    externalId
                );

                // 6. Envia e-mail ao admin APÓS persistência bem-sucedida
                await SendAdminNotificationEmailAsync(
                    adminEmail: await userContext.GetCurrentEmail(),
                    planName: localPlan.Name,
                    externalId: externalId,
                    changes: changes
                );
            }
            else
            {
                logger.LogInformation(
                    "Nenhuma atualização necessária para o plano {ExternalId}. Os dados estão sincronizados.",
                    externalId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao processar atualização do plano {ExternalId}",
                externalId
            );
            throw; // Rollback automático
        }
    }

    private async Task SendAdminNotificationEmailAsync(
        string adminEmail,
        string planName,
        string externalId,
        List<string> changes
    )
    {
        var viewModel = new PlanUpdateAdminNotificationViewModel(
            PlanName: planName,
            ExternalId: externalId,
            Changes: changes,
            DashboardUrl: $"{_generalSettings.BaseUrl}/Admin/Dashboard"
        );

        await SendEmailFromTemplateAsync(
            recipientEmail: adminEmail,
            subject: $"[Alerta] O Plano '{planName}' foi atualizado automaticamente",
            viewPath: "Pages/EmailTemplates/PlanUpdate/AdminNotification.cshtml",
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

            await emailSenderService.SendEmailAsync(
                recipientEmail,
                subject,
                htmlBody,
                plainTextBody
            );

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