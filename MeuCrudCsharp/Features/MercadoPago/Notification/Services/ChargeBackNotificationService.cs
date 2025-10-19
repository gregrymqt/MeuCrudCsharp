using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

public class ChargeBackNotificationService : IChargeBackNotificationService
{
    private readonly ApiDbContext _context;
    private readonly ILogger<ChargeBackNotificationService> _logger;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
    private readonly GeneralSettings _generalSettings;

    public ChargeBackNotificationService(
        ApiDbContext context,
        ILogger<ChargeBackNotificationService> logger,
        IEmailSenderService emailSenderService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IOptions<GeneralSettings> generalSettings
    )
    {
        _context = context;
        _logger = logger;
        _emailSenderService = emailSenderService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _generalSettings = generalSettings.Value;
    }

    public async Task VerifyAndProcessChargeBackAsync(ChargebackNotificationPayload chargebackData)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation(
                "Iniciando processamento de chargeback para o Payment ID: {PaymentId}",
                chargebackData.PaymentId
            );

            // 1. Localiza o pagamento original e o usuário associado.
            var payment = await _context
                .Payments.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ExternalId == chargebackData.PaymentId.ToString());

            if (payment?.User == null)
            {
                _logger.LogWarning(
                    "Nenhum pagamento ou usuário encontrado para o Payment ID: {PaymentId}. O chargeback será registrado sem associação de usuário.",
                    chargebackData.PaymentId
                );
            }
            else
            {
                // 2. Atualiza o status do pagamento para "chargeback".
                payment.Status = "chargeback";
                _logger.LogInformation(
                    "Status do pagamento {PaymentId} atualizado para 'chargeback'.",
                    payment.Id
                );

                // 3. Se houver uma assinatura associada, revoga o acesso.
                var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                    s.Id == payment.SubscriptionId
                );

                if (subscription != null)
                {
                    subscription.Status = "cancelled";
                    _logger.LogInformation(
                        "Assinatura {SubscriptionId} associada foi cancelada devido ao chargeback.",
                        subscription.Id
                    );
                }
            }

            // 4. Salva o registro do chargeback no banco de dados.
            var newChargeback = new Chargeback
            {
                ChargebackId = chargebackData.Id,
                PaymentId = chargebackData.PaymentId,
                UserId = payment?.UserId, // Associa ao usuário se encontrado
                Status = ChargebackStatus.Novo,
                // O valor (Amount) pode ser buscado da API do MP posteriormente, se necessário.
            };

            _context.Chargebacks.Add(newChargeback);
            _logger.LogInformation(
                "Novo registro de chargeback (ID: {ChargebackId}) criado no banco de dados.",
                chargebackData.Id
            );

            await _context.SaveChangesAsync();

            // 5. Envia um e-mail de notificação para o usuário, se ele foi encontrado.
            if (payment?.User != null)
            {
                await SendChargebackReceivedEmailAsync(payment.User, chargebackData.Id);
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar o chargeback para o Payment ID: {PaymentId}. A transação será revertida.",
                chargebackData.PaymentId
            );
            await transaction.RollbackAsync();
            throw; // Relança para o Hangfire tentar novamente.
        }
    }

    private async Task SendChargebackReceivedEmailAsync(Users user, long chargebackId)
    {
        var viewModel = new ChargebackReceivedEmailViewModel(
            userName: user.Name,
            chargebackId: chargebackId,
            supportUrl: $"{_generalSettings.BaseUrl}/Support/Contact/index.cshtml"
        );

        var htmlBody = await _razorViewToStringRenderer.RenderViewToStringAsync(
            "Features/Shared/EmailTemplates/ChargebackReceived/index.cshtml",
            viewModel
        );
        await _emailSenderService.SendEmailAsync(
            user.Email,
            $"Notificação de Chargeback (ID: {chargebackId})",
            htmlBody,
            string.Empty
        );
        _logger.LogInformation(
            "E-mail de notificação de chargeback enviado para {UserEmail}.",
            user.Email
        );
    }
}
