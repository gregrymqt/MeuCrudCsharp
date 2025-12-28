using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

public class CardUpdateNotificationService : ICardUpdateNotificationService
{
    private readonly ILogger<CardUpdateNotificationService> _logger;
    private readonly ApiDbContext _context;
    private readonly IClientService _clientService;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
    private readonly GeneralSettings _generalSettings;

    public CardUpdateNotificationService(
        ILogger<CardUpdateNotificationService> logger,
        ApiDbContext context,
        IClientService clientService,
        IEmailSenderService emailSenderService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IOptions<GeneralSettings> generalSettings
    )
    {
        _logger = logger;
        _context = context;
        _clientService = clientService;
        _emailSenderService = emailSenderService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _generalSettings = generalSettings.Value;
    }

    public async Task VerifyAndProcessCardUpdate(CardUpdateNotificationPayload cardUpdatePayload)
    {
        _logger.LogInformation(
            "Iniciando processamento de atualização de cartão para o CustomerId: {CustomerId}",
            cardUpdatePayload.CustomerId
        );

        // 1. Busca a assinatura ativa do usuário pelo CustomerId
        var subscription = await _context
            .Subscriptions.Include(s => s.User)
            .FirstOrDefaultAsync(s =>
                s.User.CustomerId == cardUpdatePayload.CustomerId
                && (s.Status == "active" || s.Status == "paused")
            );

        if (subscription?.User == null)
        {
            _logger.LogWarning(
                "Nenhuma assinatura ativa encontrada para o CustomerId: {CustomerId}. O processo será ignorado.",
                cardUpdatePayload.CustomerId
            );
            return;
        }

        // 2. Busca os detalhes do novo cartão na API do Mercado Pago
        _logger.LogInformation(
            "Buscando detalhes do cartão {CardId} para o cliente {CustomerId}",
            cardUpdatePayload.NewCardId,
            cardUpdatePayload.CustomerId
        );
        var cardDetails = await _clientService.GetCardInCustomerAsync(
            cardUpdatePayload.CustomerId,
            cardUpdatePayload.NewCardId.ToString()
        );

        if (string.IsNullOrEmpty(cardDetails?.LastFourDigits))
        {
            throw new AppServiceException(
                $"Não foi possível obter os detalhes do cartão {cardUpdatePayload.NewCardId} do Mercado Pago."
            );
        }

        // 3. Atualiza a assinatura com os novos dados do cartão
        subscription.CardTokenId = cardDetails.Id;
        subscription.LastFourCardDigits = cardDetails.LastFourDigits;
        subscription.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Assinatura {SubscriptionId} atualizada com o novo cartão de final {LastFourDigits}.",
            subscription.Id,
            cardDetails.LastFourDigits
        );

        // 4. Envia e-mail de notificação para o usuário
        await SendCardUpdateEmailAsync(subscription.User, cardDetails.LastFourDigits);
    }

    private async Task SendCardUpdateEmailAsync(Users user, string lastFourDigits)
    {
        var viewModel = new CardUpdateEmailViewModel(
            UserName: user.Name,
            LastFourDigits: lastFourDigits,
            // Digamos que no React a rota seja: <Route path="/perfil" ... />
            AccountUrl: $"{_generalSettings.FrontendUrl}/perfil"
        );

        await SendEmailFromTemplateAsync(
            recipientEmail: user.Email,
            subject: "Seu método de pagamento foi atualizado",
            viewPath: "Pages/EmailTemplates/CardUpdate/index.cshtml",
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
            "Renderizando template de e-mail '{ViewPath}' para {RecipientEmail}.",
            viewPath,
            recipientEmail
        );
        var htmlBody = await _razorViewToStringRenderer.RenderViewToStringAsync(viewPath, model);
        var plainTextBody =
            $"Seu método de pagamento foi atualizado com sucesso. Para mais detalhes, acesse sua conta.";

        await _emailSenderService.SendEmailAsync(recipientEmail, subject, htmlBody, plainTextBody);
        _logger.LogInformation(
            "E-mail de atualização de cartão enviado para {RecipientEmail}.",
            recipientEmail
        );
    }
}
