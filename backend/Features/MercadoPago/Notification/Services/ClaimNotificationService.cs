using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Claims.Interfaces;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Models.Enums; // Ajuste o namespace das DTOs

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

public class ClaimNotificationService : IClaimNotificationService
{
    private readonly ApiDbContext _context;
    private readonly ILogger<ClaimNotificationService> _logger;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
    private readonly GeneralSettings _generalSettings;
    private readonly IUserRepository _userRepository;
    private readonly IMercadoPagoIntegrationService _mpIntegrationService;

    public ClaimNotificationService(
        ApiDbContext context,
        ILogger<ClaimNotificationService> logger,
        IEmailSenderService emailSenderService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IOptions<GeneralSettings> generalSettings,
        IUserRepository userRepository,
        IMercadoPagoIntegrationService mpIntegrationService
    )
    {
        _context = context;
        _logger = logger;
        _emailSenderService = emailSenderService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _generalSettings = generalSettings.Value;
        _userRepository = userRepository;
        _mpIntegrationService = mpIntegrationService;
    }

    public async Task VerifyAndProcessClaimAsync(ClaimNotificationPayload claimPayload)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Iniciando processamento da Claim ID: {ClaimId}", claimPayload.Id);

            if (claimPayload == null || string.IsNullOrEmpty(claimPayload.Id))
            {
                _logger.LogWarning("Payload ou Id inválido.");
                return;
            }

            if (!long.TryParse(claimPayload.Id, out long mpClaimId))
            {
                _logger.LogError("ID da Claim não é um número válido: {Id}", claimPayload.Id);
                return;
            }

            var claimDetails = await _mpIntegrationService.GetClaimByIdAsync(mpClaimId);

            if (claimDetails == null)
            {
                _logger.LogError("Não foi possível obter detalhes da Claim {Id} na API do MP.", mpClaimId);
                return;
            }

            string resourceId = claimDetails.ResourceId;

            // CORREÇÃO: Usar o Enum diretamente, sem operador ?? com string
            // Se vier nulo, assumimos Payment como fallback, mas mantendo o tipo Enum
            ClaimResource resourceTypeEnum = claimDetails.Resource;

            _logger.LogInformation("Claim vinculada ao Recurso: {ResourceId}, Tipo: {Type}", resourceId, resourceTypeEnum);

            Users? user = null;

            // CORREÇÃO: Comparação usando Enum, não string "contains"
            if (resourceTypeEnum == ClaimResource.Payment)
            {
                var payment = await _context.Payments.AsNoTracking()
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == resourceId);
                user = payment?.User;
            }
            // Tratamento para Assinatura (Subscription)
            // O MP as vezes manda o ID da assinatura no resource se a disputa for sobre o plano
            else
            {
                var subscription = await _context.Subscriptions.AsNoTracking()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.ExternalId == resourceId);
                user = subscription?.User;
            }

            if (user == null)
            {
                _logger.LogWarning("Nenhum usuário encontrado para o Recurso ID '{ResourceId}'.", resourceId);
            }

            // Verifica se já existe (usando long)
            bool jaExiste = await _context.Claims.AnyAsync(c => c.MpClaimId == mpClaimId);

            if (!jaExiste)
            {
                var newClaimRecord = new Models.Claims
                {
                    MpClaimId = mpClaimId,           // CORREÇÃO: Passando long
                    ResourceId = resourceId,         // CORREÇÃO: Nome correto da propriedade (era ResorceId)

                    // CORREÇÃO: Atribuindo Enum ClaimType direto (era string "claim_received")
                    Type = claimDetails.Type,

                    // CORREÇÃO: Nome correto (ResourceType) e atribuindo Enum (era TypeResource)
                    ResourceType = resourceTypeEnum,

                    UserId = user?.Id.ToString(),
                    DataCreated = DateTime.UtcNow,

                    Status = InternalClaimStatus.Novo,

                    // SUGESTÃO: Se você adicionou o campo CurrentStage na classe Claims, use aqui:
                    CurrentStage = claimDetails.Stage // (Enum ClaimStage)
                };

                _context.Claims.Add(newClaimRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim ID {ClaimId} salva no banco com sucesso.", claimPayload.Id);
            }

            if (user != null)
            {
                // CORREÇÃO: Passando long para o método de email (se ele foi atualizado) ou payload.Id string
                // Se o método espera string, use claimPayload.Id. Se espera long, use mpClaimId.
                // Aqui mantive payload.Id (string) assumindo que o método de email não mudou a assinatura.
                await SendClaimReceivedEmailAsync(user, mpClaimId);
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao processar Claim {Id}", claimPayload.Id);
            throw;
        }
    }

    private async Task SendClaimReceivedEmailAsync(Users user, long claimId)
    {
        var viewModel = new ClaimReceivedEmailViewModel(
            userName: user.Name,
            claimId: claimId,
            accountUrl: $"{_generalSettings.BaseUrl}/Profile/User/index.cshtml",
            supportUrl: $"{_generalSettings.BaseUrl}/Support/Contact/index.cshtml"
        );

        await SendEmailFromTemplateAsync(
            recipientEmail: user.Email,
            subject: $"Recebemos sua solicitação (ID: {claimId})",
            viewPath: "Pages/EmailTemplates/ClaimReceived/index.cshtml", // Crie este template
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
        _logger.LogInformation(
            "Iniciando montagem de e-mail a partir do template '{ViewPath}' para {RecipientEmail}.",
            viewPath,
            recipientEmail
        );

        try
        {
            // Usa o modelo que foi passado como parâmetro.
            var htmlBody = await _razorViewToStringRenderer.RenderViewToStringAsync(
                viewPath,
                model
            );

            var plainTextBody =
                $"Assunto: {subject}. Para visualizar esta mensagem, utilize um leitor de e-mail compatível com HTML.";

            await _emailSenderService.SendEmailAsync(
                recipientEmail,
                subject,
                htmlBody,
                plainTextBody
            );

            _logger.LogInformation(
                "E-mail para {RecipientEmail} enviado com sucesso.",
                recipientEmail
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha no processo de montagem e envio de e-mail para {RecipientEmail}.",
                recipientEmail
            );
            throw;
        }
    }

}
