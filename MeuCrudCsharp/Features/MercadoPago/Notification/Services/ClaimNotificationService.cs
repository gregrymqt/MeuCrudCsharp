using System;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services;

public class ClaimNotificationService : IClaimNotificationService
{
    private readonly ApiDbContext _context;
    private readonly ILogger<ClaimNotificationService> _logger;
    private readonly IEmailSenderService _emailSenderService;
    private readonly IRazorViewToStringRenderer _razorViewToStringRenderer;
    private readonly GeneralSettings _generalSettings;
    private readonly IUserRepository _userRepository;

    public ClaimNotificationService(
        ApiDbContext context,
        ILogger<ClaimNotificationService> logger,
        IEmailSenderService emailSenderService,
        IRazorViewToStringRenderer razorViewToStringRenderer,
        IOptions<GeneralSettings> generalSettings,
        IUserRepository userRepository
    )
    {
        _context = context;
        _logger = logger;
        _emailSenderService = emailSenderService;
        _razorViewToStringRenderer = razorViewToStringRenderer;
        _generalSettings = generalSettings.Value;
        _userRepository = userRepository;
    }

    public async Task VerifyAndProcessClaimAsync(ClaimNotificationPayload claimPayload)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _logger.LogInformation(
                "Iniciando processamento da notificação para a Claim ID: {ClaimId}",
                claimPayload.Id
            );

            // Passo 1: Extrair o ID do recurso (pagamento ou assinatura) da URL.
            var resourceId = ExtractIdFromResource(claimPayload.Resource);
            if (string.IsNullOrEmpty(resourceId))
            {
                _logger.LogWarning(
                    "Não foi possível extrair o ID do recurso da URL '{Resource}'. Não é possível associar a um usuário.",
                    claimPayload.Resource
                );
                await transaction.CommitAsync(); // Comita a transação vazia para não reprocessar.
                return;
            }

            var typePayment =
                claimPayload.Resource?.Contains("/payments/") == true ? "payment" : "subscription";
            // Passo 2: Encontrar o usuário associado, seja por pagamento ou por assinatura.
            Users? user = null;
            if (typePayment == "payment")
            {
                _logger.LogInformation(
                    "A claim refere-se a um pagamento. Buscando pelo Payment ID: {ResourceId}",
                    resourceId
                );
                var payment = await _context
                    .Payments.AsNoTracking()
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == resourceId);
                user = payment?.User;
            }
            else if (typePayment == "subscription")
            {
                _logger.LogInformation(
                    "A claim refere-se a uma assinatura. Buscando pelo External ID: {ResourceId}",
                    resourceId
                );
                var subscription = await _context
                    .Subscriptions.AsNoTracking()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.ExternalId == resourceId);
                user = subscription?.User;
            }

            if (user == null)
            {
                _logger.LogWarning(
                    "Nenhum usuário encontrado para o recurso com ID '{ResourceId}' extraído da claim. O e-mail não será enviado.",
                    resourceId
                );
            }

            // Passo 3: Salvar o registro da notificação da claim no banco de dados.
            var newClaimRecord = new Claims
            {
                NotificationId = claimPayload.Id,
                ClaimId = resourceId,
                Type = "claim_received",
                TypePayment = typePayment,
            };

            _context.Claims.Add(newClaimRecord);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Registro da notificação de Claim ID {ClaimId} salvo no banco de dados.",
                claimPayload.Id
            );

            // Passo 4: Se o usuário foi encontrado, enviar o e-mail de notificação.
            if (user != null)
            {
                await SendClaimReceivedEmailAsync(user, claimPayload.Id);
            }

            await transaction.CommitAsync();
            _logger.LogInformation(
                "Processamento da Claim ID {ClaimId} concluído e transação comitada.",
                claimPayload.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar a Claim ID {ClaimId}. A transação será revertida.",
                claimPayload.Id
            );
            await transaction.RollbackAsync();
            throw; // Relança a exceção para que o Hangfire possa tentar novamente.
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

    private string? ExtractIdFromResource(string? resourceUrl)
    {
        if (string.IsNullOrEmpty(resourceUrl))
            return null;

        // Extrai o último segmento da URL, que geralmente é o ID.
        // Ex: "https://api.mercadopago.com/v1/payments/12345" -> "12345"
        // Ex: "https://api.mercadopago.com/preapproval/abcdef123" -> "abcdef123"
        var segments = resourceUrl.Split('/');
        return segments.LastOrDefault();
    }
}
