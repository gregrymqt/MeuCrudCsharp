using Microsoft.Extensions.Options;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Models.Enums;
using MeuCrudCsharp.Features.Shared.Work;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

/// <summary>
/// Service responsável por processar notificações de Claims do Mercado Pago.
/// Usa o padrão Unit of Work para garantir transações atômicas.
/// </summary>
public class ClaimNotificationService(
    IClaimRepository claimRepository,
    IPaymentRepository paymentRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ClaimNotificationService> logger,
    IEmailSenderService emailSenderService,
    IRazorViewToStringRenderer razorViewToStringRenderer,
    IOptions<GeneralSettings> generalSettings,
    IMercadoPagoIntegrationService mpIntegrationService)
    : IClaimNotificationService
{
    private readonly GeneralSettings _generalSettings = generalSettings.Value;

    public async Task VerifyAndProcessClaimAsync(ClaimNotificationPayload claimPayload)
    {
        try
        {
            logger.LogInformation("Iniciando processamento da Claim ID: {ClaimId}", claimPayload.Id);

            // Validação de entrada
            if (string.IsNullOrWhiteSpace(claimPayload.Id))
            {
                logger.LogWarning("Id da claim inválido.");
                return;
            }

            if (!long.TryParse(claimPayload.Id, out var mpClaimId))
            {
                logger.LogError("ID da Claim não é um número válido: {Id}", claimPayload.Id);
                return;
            }

            // 1. Busca detalhes na API do Mercado Pago
            var claimDetails = await mpIntegrationService.GetClaimByIdAsync(mpClaimId);

            if (claimDetails == null)
            {
                logger.LogError("Não foi possível obter detalhes da Claim {Id} na API do MP.", mpClaimId);
                return;
            }

            var resourceId = claimDetails.ResourceId;
            var resourceTypeEnum = claimDetails.Resource;

            logger.LogInformation("Claim vinculada ao Recurso: {ResourceId}, Tipo: {Type}", resourceId, resourceTypeEnum);

            // 2. Localiza o usuário através do Payment ou Subscription
            Users? user;

            if (resourceTypeEnum == ClaimResource.Payment)
            {
                var payment = await paymentRepository.GetByExternalIdWithUserAsync(resourceId);
                user = payment?.User;
            }
            else
            {
                // Para outros tipos de recursos (como assinatura), tenta buscar subscription
                var subscription = await subscriptionRepository.GetByIdAsync(resourceId);
                user = subscription?.User;
            }

            if (user == null)
            {
                logger.LogWarning("Nenhum usuário encontrado para o Recurso ID '{ResourceId}'.", resourceId);
            }

            // 3. Verifica se a claim já existe no banco local
            var existingClaim = await claimRepository.GetByMpClaimIdAsync(mpClaimId);

            if (existingClaim == null)
            {
                // CREATE - Nova claim
                var newClaimRecord = new Models.Claims
                {
                    MpClaimId = mpClaimId,
                    ResourceId = resourceId,
                    Type = claimDetails.Type,
                    ResourceType = resourceTypeEnum,
                    UserId = user?.Id.ToString(),
                    DataCreated = DateTime.UtcNow,
                    Status = InternalClaimStatus.Novo,
                    CurrentStage = claimDetails.Stage
                };

                await claimRepository.AddAsync(newClaimRecord);
                logger.LogInformation("Nova Claim ID {ClaimId} marcada para inserção.", mpClaimId);
            }
            else
            {
                // UPDATE - Atualiza status se necessário
                logger.LogInformation("Claim ID {ClaimId} já existe. Verificando atualizações.", mpClaimId);
                
                // Aqui você pode adicionar lógica para atualizar o status baseado no stage do MP
                // Por exemplo:
                // existingClaim.CurrentStage = claimDetails.Stage;
                // _claimRepository.Update(existingClaim);
            }

            // 4. Persiste todas as mudanças (Commit da Transação)
            await unitOfWork.CommitAsync();

            // 5. Envia e-mail (somente após persistência bem-sucedida)
            if (user != null && existingClaim == null) // Envia apenas para claims novas
            {
                await SendClaimReceivedEmailAsync(user, mpClaimId);
            }

            logger.LogInformation("Claim {Id} processada com sucesso.", mpClaimId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar Claim {Id}", claimPayload.Id);
            throw;
        }
    }

    private async Task SendClaimReceivedEmailAsync(Users user, long claimId)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            logger.LogWarning("Email do usuário inválido. Não foi possível enviar email.");
            return;
        }

        var viewModel = new ClaimReceivedEmailViewModel(
            userName: user.Name ?? "Cliente",
            claimId: claimId,
            accountUrl: $"{_generalSettings.BaseUrl}/Profile/User/index.cshtml",
            supportUrl: $"{_generalSettings.BaseUrl}/Support/Contact/index.cshtml"
        );

        await SendEmailFromTemplateAsync(
            recipientEmail: user.Email,
            subject: $"Notificação de Reclamação (ID: {claimId})",
            viewPath: "Pages/EmailTemplates/ClaimReceived/index.cshtml",
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
            // Usa o modelo que foi passado como parâmetro.
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
