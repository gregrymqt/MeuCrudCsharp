using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services
{
    public class SubscriptionRenewalNotificationService : ISubscriptionNotificationService
    {
        private readonly ILogger<SubscriptionRenewalNotificationService> _logger;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
        private readonly GeneralSettings _generalSettings;

        public SubscriptionRenewalNotificationService(
            ILogger<SubscriptionRenewalNotificationService> logger,
            IEmailSenderService emailSenderService,
            IRazorViewToStringRenderer razorViewToStringRenderer,
            IOptions<GeneralSettings> generalSettings
        )
        {
            _logger = logger;
            _emailSenderService = emailSenderService;
            _razorViewToStringRenderer = razorViewToStringRenderer;
            _generalSettings = generalSettings.Value;
        }

        public async Task ProcessRenewalAsync(Subscription subscription)
        {
            _logger.LogInformation(
                "Iniciando regras de negócio para renovar a assinatura {SubscriptionId}.",
                subscription.Id
            );

            var planInterval = subscription.Plan.FrequencyInterval;
            var planFrequency = subscription.Plan.FrequencyType;

            DateTime newExpirationDate = subscription.CurrentPeriodEndDate;
            if (planFrequency == PlanFrequencyType.Months)
            {
                newExpirationDate = newExpirationDate.AddMonths(planInterval);
            }
            else if (planFrequency == PlanFrequencyType.Days)
            {
                newExpirationDate = newExpirationDate.AddDays(planInterval);
            }

            subscription.CurrentPeriodEndDate = newExpirationDate;
            subscription.Status = "active";

            _logger.LogInformation(
                "Regra de negócio aplicada. Nova data de validade para a assinatura {SubscriptionId} é {NewExpirationDate}",
                subscription.Id,
                newExpirationDate
            );

            var viewModel = new RenewalEmailViewModel(
                userName: subscription.User.Name,
                planName: subscription.Plan.Name,
                newExpirationDate: subscription.CurrentPeriodEndDate,
                transactionAmount: subscription.Plan.TransactionAmount,
                accountUrl: $"{_generalSettings.BaseUrl}/Profile/User/index.cshtml",
                supportUrl: $"{_generalSettings.BaseUrl}/Support/Contact/index.cshtml"
            );

            await SendEmailFromTemplateAsync(
                recipientEmail: subscription.User.Email,
                subject: "Sua assinatura foi renovada com sucesso!",
                viewPath: "Pages/EmailTemplates/Renewal/index.cshtml",
                model: viewModel
            );

            await Task.CompletedTask;
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
}
