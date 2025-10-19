using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

public class SubscriptionCreateNotificationService : ISubscriptionCreateNotificationService
{
    private readonly ILogger<SubscriptionCreateNotificationService> _logger;
    private readonly IEmailSenderService _emailService;
    private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
    private readonly GeneralSettings _generalSettings;

    public SubscriptionCreateNotificationService(
        ILogger<SubscriptionCreateNotificationService> logger,
        IEmailSenderService emailService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IOptions<GeneralSettings> generalSettings
    )
    {
        _logger = logger;
        _emailService = emailService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _generalSettings = generalSettings.Value;
    }

    public async Task VerifyAndProcessSubscriptionAsync(Subscription subscription)
    {
        _logger.LogInformation(
            "Iniciando regras de negócio para assinatura {SubscriptionId}.",
            subscription.Id
        );

        subscription.Status = "ativo";

        var viewModel = new SubscriptionCreateEmailViewModel(
            userName: subscription.User.Name,
            planName: subscription.Plan.Name,
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

            await _emailService.SendEmailAsync(recipientEmail, subject, htmlBody, plainTextBody);

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
