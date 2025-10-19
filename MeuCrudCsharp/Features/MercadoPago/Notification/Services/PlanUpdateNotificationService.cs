using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

public class PlanUpdateNotificationService : IPlanUpdateNotificationService
{
    private readonly ApiDbContext _context;
    private readonly IMercadoPagoPlanService _mercadoPagoApiService;
    private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
    private readonly IEmailSenderService _emailSenderService;
    private readonly GeneralSettings _generalSettings;
    private readonly ILogger<PlanUpdateNotificationService> _logger;
    private readonly IUserContext _userContext;

    public PlanUpdateNotificationService(
        ApiDbContext context,
        IMercadoPagoPlanService mercadoPagoApiService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IEmailSenderService emailSenderService,
        IOptions<GeneralSettings> generalSettings,
        ILogger<PlanUpdateNotificationService> logger,
        IUserContext userContext
    )
    {
        _context = context;
        _mercadoPagoApiService = mercadoPagoApiService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _emailSenderService = emailSenderService;
        _generalSettings = generalSettings.Value;
        _logger = logger;
        _userContext = userContext;
    }

    public async Task VerifyAndProcessPlanUpdate(string externalId)
    {
        _logger.LogInformation(
            "Iniciando verificação e processamento para o plano com ExternalId: {ExternalId}",
            externalId
        );

        // 1. Busca o plano na API do Mercado Pago
        var mpPlan = await _mercadoPagoApiService.GetPlanByExternalIdAsync(externalId);
        if (mpPlan?.Id is null || mpPlan.AutoRecurring is null)
        {
            _logger.LogWarning(
                "Não foi possível obter os detalhes do plano da API do Mercado Pago para o ExternalId: {ExternalId}",
                externalId
            );
            return;
        }

        // 2. Busca o plano no banco de dados local (sem tracking, pois o job já travou a linha)
        var localPlan = await _context
            .Plans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ExternalPlanId == externalId);

        if (localPlan is null)
        {
            _logger.LogWarning(
                "Plano com ExternalId {ExternalId} encontrado na API do MP, mas não no banco de dados local.",
                externalId
            );
            return;
        }

        bool needsUpdate = false;
        var changes = new List<string>();

        var statusMapping = new Dictionary<string, bool>
        {
            { "active", true },
            { "cancelled", false },
        };

        // 3. Compara os valores e atualiza se necessário

        if (localPlan.IsActive != statusMapping[mpPlan.Status!])
        {
            _logger.LogInformation(
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
            _logger.LogInformation(
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
            _logger.LogInformation(
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
            // Envia e-mail para o administrador notificando sobre a mudança
            await SendAdminNotificationEmailAsync(
                adminEmail: await _userContext.GetCurrentEmail(),
                planName: localPlan.Name,
                externalId: externalId,
                changes: changes
            );

            _context.Plans.Update(localPlan);
            _logger.LogInformation(
                "Plano {ExternalId} foi atualizado para refletir os dados do Mercado Pago.",
                externalId
            );
        }
        else
        {
            _logger.LogInformation(
                "Nenhuma atualização necessária para o plano {ExternalId}. Os dados estão sincronizados.",
                externalId
            );
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
        _logger.LogInformation(
            "Iniciando montagem de e-mail a partir do template '{ViewPath}' para {RecipientEmail}.",
            viewPath,
            recipientEmail
        );

        try
        {
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
