using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Services; // Certifique-se que este using existe
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
    private readonly IMercadoPagoChargebackIntegrationService _mpIntegrationService;

    public ChargeBackNotificationService(
        ApiDbContext context,
        ILogger<ChargeBackNotificationService> logger,
        IEmailSenderService emailSenderService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IOptions<GeneralSettings> generalSettings,
        IMercadoPagoChargebackIntegrationService mpIntegrationService
    )
    {
        _context = context;
        _logger = logger;
        _emailSenderService = emailSenderService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _generalSettings = generalSettings.Value;
        _mpIntegrationService = mpIntegrationService;
    }

    public async Task VerifyAndProcessChargeBackAsync(ChargebackNotificationPayload chargebackData)
    {
        // 1. Busca detalhes na API do MP
        var mpDetails = await _mpIntegrationService.GetChargebackDetailsFromApiAsync(
            chargebackData.Id
        );

        if (mpDetails == null)
        {
            throw new Exception(
                $"Chargeback {chargebackData.Id} não encontrado na API do Mercado Pago. Job será retentado."
            );
        }

        // 2. Extrai o ID do Pagamento (MP)
        var paymentIdStr = mpDetails.Payments?.FirstOrDefault()?.Id;

        if (string.IsNullOrEmpty(paymentIdStr))
        {
            _logger.LogError(
                "Chargeback {Id} não possui pagamentos vinculados na resposta da API.",
                chargebackData.Id
            );
            return;
        }

        // Variáveis seguras convertidas para long
        long mpPaymentId = long.Parse(paymentIdStr);
        long mpChargebackId = long.Parse(mpDetails.Id);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation(
                "Processando Chargeback {CId} para Pagamento {PId}",
                mpChargebackId,
                mpPaymentId
            );

            // 3. Localiza o pagamento no NOSSO banco pelo ID externo (MP)
            var payment = await _context
                .Payments.Include(p => p.User)
                .FirstOrDefaultAsync(p => p.ExternalId == paymentIdStr);

            if (payment == null)
            {
                _logger.LogWarning("Pagamento {PId} não encontrado na base local.", mpPaymentId);
            }
            else
            {
                // Atualiza status do pagamento local
                payment.Status = "chargeback";

                // --- CORREÇÃO 1: HasValue removido, verifica se string não é nula/vazia ---
                if (!string.IsNullOrEmpty(payment.SubscriptionId))
                {
                    var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                        s.Id == payment.SubscriptionId
                    );

                    if (subscription != null)
                    {
                        subscription.Status = "cancelled";
                        _logger.LogInformation(
                            "Assinatura {SubId} cancelada por chargeback.",
                            subscription.Id
                        );
                    }
                }
            }

            // 4. Verifica se o Chargeback já existe (Upsert)
            var existingChargeback = await _context.Chargebacks.FirstOrDefaultAsync(c =>
                c.ChargebackId == mpChargebackId
            );

            if (existingChargeback == null)
            {
                // CREATE
                var newChargeback = new Chargeback
                {
                    ChargebackId = mpChargebackId,

                    // --- CORREÇÃO 2: Usamos a variável long mpPaymentId, não o GUID do banco ---
                    PaymentId = mpPaymentId,

                    UserId = payment?.UserId,
                    Amount = mpDetails.Amount,
                    Status = ChargebackStatus.Novo,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Chargebacks.Add(newChargeback);
            }
            else
            {
                // UPDATE
                existingChargeback.Amount = mpDetails.Amount;
                // Atualiza notas internas se quiser
                // existingChargeback.InternalNotes += $" | Atualizado via Webhook em {DateTime.Now}";
                _context.Chargebacks.Update(existingChargeback);
            }

            await _context.SaveChangesAsync();

            // 5. Envia E-mail
            if (payment?.User != null && !string.IsNullOrEmpty(payment.User.Email))
            {
                await SendChargebackReceivedEmailAsync(payment.User, mpChargebackId);
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Chargeback {Id} processado com sucesso.", mpChargebackId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao salvar Chargeback {Id}.", mpChargebackId);
            throw;
        }
    }

    private async Task SendChargebackReceivedEmailAsync(Users user, long chargebackId)
    {
        if (user == null || string.IsNullOrEmpty(user.Email))
            return;

        var viewModel = new ChargebackReceivedEmailViewModel(
            userName: user.Name ?? "Cliente",
            chargebackId: chargebackId,
            supportUrl: $"{_generalSettings.BaseUrl}/Support/Contact/index.cshtml"
        );

        var htmlBody = await _razorViewToStringRenderer.RenderViewToStringAsync(
            "Features/Shared/EmailTemplates/ChargebackReceived/index.cshtml",
            viewModel
        );

        await _emailSenderService.SendEmailAsync(
            user.Email,
            $"Notificação de Contestação (ID: {chargebackId})",
            htmlBody,
            string.Empty
        );
    }
}
