using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.MercadoPago.Chargebacks.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Features.Shared.Work;
using MeuCrudCsharp.Models;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

/// <summary>
/// Service responsável por processar notificações de Chargeback do Mercado Pago.
/// Coordena múltiplos repositories (Payment, Subscription, Chargeback) e usa UnitOfWork
/// para garantir transações atômicas.
/// </summary>
public class ChargeBackNotificationService(
    IChargebackRepository chargebackRepository,
    IPaymentRepository paymentRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ChargeBackNotificationService> logger,
    IEmailSenderService emailSenderService,
    IRazorViewToStringRenderer razorViewToStringRenderer,
    IOptions<GeneralSettings> generalSettings,
    IMercadoPagoChargebackIntegrationService mpIntegrationService)
    : IChargeBackNotificationService
{
    // ADICIONADO: Repositórios e UoW

    private readonly GeneralSettings _generalSettings = generalSettings.Value;

    public async Task VerifyAndProcessChargeBackAsync(ChargebackNotificationPayload chargebackData)
    {
        // 1. Busca detalhes na API do MP
        var mpDetails = await mpIntegrationService.GetChargebackDetailsFromApiAsync(
            chargebackData.Id
        );

        if (mpDetails == null)
            throw new Exception(
                $"Chargeback {chargebackData.Id} não encontrado na API do Mercado Pago."
            );

        var paymentIdStr = mpDetails.Payments?.FirstOrDefault()?.Id;
        if (string.IsNullOrEmpty(paymentIdStr))
        {
            logger.LogError("Chargeback {Id} sem pagamentos vinculados.", chargebackData.Id);
            return;
        }

        var mpPaymentId = long.Parse(paymentIdStr);
        var mpChargebackId = long.Parse(mpDetails.Id);

        // --- ALTERAÇÃO: Transação via UnitOfWork (ou implícita no Commit) ---
        // Se o seu UoW não tiver BeginTransaction explícito, apenas o CommitAsync no final garante a atomicidade do SaveChanges
        try
        {
            logger.LogInformation(
                "Processando Chargeback {CId} para Pagamento {PId}",
                mpChargebackId,
                mpPaymentId
            );

            // 3. Localiza pagamento via Repository
            // Substitui: _context.Payments.Include(p => p.User).FirstOrDefaultAsync(...)
            var payment = await paymentRepository.GetByExternalIdWithUserAsync(paymentIdStr);

            if (payment == null)
            {
                logger.LogWarning("Pagamento {PId} não encontrado na base local.", mpPaymentId);
            }
            else
            {
                // Atualiza Pagamento
                payment.Status = "chargeback";
                paymentRepository.Update(payment); // Marca explicitamente para update

                // Atualiza Assinatura se existir
                if (!string.IsNullOrEmpty(payment.SubscriptionId))
                {
                    // Substitui: _context.Subscriptions.FirstOrDefaultAsync(...)
                    var subscription = await subscriptionRepository.GetByIdAsync(
                        payment.SubscriptionId
                    );

                    if (subscription != null)
                    {
                        subscription.Status = "cancelled";
                        subscriptionRepository.Update(subscription);
                        logger.LogInformation(
                            "Assinatura {SubId} cancelada por chargeback.",
                            subscription.Id
                        );
                    }
                }
            }

            // 4. Verifica/Cria Chargeback via Repository
            // Substitui: _context.Chargebacks.FirstOrDefaultAsync(...)
            var existingChargeback = await chargebackRepository.GetByExternalIdAsync(
                mpChargebackId
            );

            if (existingChargeback == null)
            {
                // CREATE
                var newChargeback = new Chargeback
                {
                    ChargebackId = mpChargebackId,
                    PaymentId = mpPaymentId, // Usa o long parseado
                    UserId = payment?.UserId,
                    Amount = mpDetails.Amount,
                    Status = ChargebackStatus.Novo,
                    CreatedAt = DateTime.UtcNow,
                };
                await chargebackRepository.AddAsync(newChargeback);
            }
            else
            {
                // UPDATE
                existingChargeback.Amount = mpDetails.Amount;
                chargebackRepository.Update(existingChargeback);
            }

            // 5. Persiste tudo (Commit da Transação)
            // Substitui: await _context.SaveChangesAsync();
            await unitOfWork.CommitAsync();

            // 6. Envia E-mail (Pós-persistência para garantir que salvou antes de avisar)
            if (payment?.User != null && !string.IsNullOrEmpty(payment.User.Email))
            {
                await SendChargebackReceivedEmailAsync(payment.User, mpChargebackId);
            }

            logger.LogInformation("Chargeback {Id} processado com sucesso.", mpChargebackId);
        }
        catch (Exception ex)
        {
            // O Rollback geralmente é automático se não der o Commit, mas pode ser explícito dependendo da sua impl do UoW
            logger.LogError(ex, "Erro ao salvar Chargeback {Id}.", mpChargebackId);
            throw;
        }
    }

    private async Task SendChargebackReceivedEmailAsync(Users user, long chargebackId)
    {
        if (string.IsNullOrEmpty(user.Email))
        {
            logger.LogWarning(
                "Usuário {UserId} sem email. Não foi possível enviar notificação de chargeback.",
                user.Id
            );
            return;
        }

        var viewModel = new ChargebackReceivedEmailViewModel(
            userName: user.Name ?? "Cliente",
            chargebackId: chargebackId,
            supportUrl: $"{_generalSettings.BaseUrl}/Support/Contact/index.cshtml"
        );

        var htmlBody = await razorViewToStringRenderer.RenderViewToStringAsync(
            "Pages/EmailTemplates/ChargebackReceived/index.cshtml",
            viewModel
        );

        await emailSenderService.SendEmailAsync(
            user.Email,
            $"Notificação de Contestação (ID: {chargebackId})",
            htmlBody,
            string.Empty
        );
    }
}