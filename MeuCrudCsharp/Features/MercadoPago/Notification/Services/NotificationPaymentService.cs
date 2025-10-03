using System;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.ViewModels;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Notification.Services
{
    /// <summary>
    /// Implementa <see cref="INotificationPayment"/> para processar notificações de pagamento.
    /// Este serviço verifica o status de um pagamento no banco de dados local e envia e-mails de confirmação ou rejeição ao usuário.
    /// </summary>
    public class NotificationPaymentService : INotificationPayment
    {
        private readonly ApiDbContext _context;
        private readonly IEmailSenderService _emailSender;
        private readonly IRazorViewToStringRenderer _razorRenderer;
        private readonly ILogger<NotificationPaymentService> _logger;
        private readonly IMercadoPagoPaymentService _mercadoPagoService;
        private readonly IRefundNotification _refundNotification;
        private readonly ISubscriptionService  _subscriptionService;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="NotificationPayment"/>.
        /// </summary>
        /// <param name="context">O contexto do banco de dados.</param>
        /// <param name="emailSender">O serviço para envio de e-mails.</param>
        /// <param name="razorRenderer">O serviço para renderizar templates Razor para string.</param>
        /// <param name="logger">O serviço de logging.</param>
        /// <param name="mercadoPagoService">O serviço de logging.</param>
        /// <param name="refundNotification">O serviço de logging.</param>
        public NotificationPaymentService(
            ApiDbContext context,
            IEmailSenderService emailSender,
            IRazorViewToStringRenderer razorRenderer,
            ILogger<NotificationPaymentService> logger,
            IMercadoPagoPaymentService mercadoPagoService, // <-- INJEÇÃO DO SERVIÇO DO MP
            IRefundNotification refundNotification, // <-- INJEÇÃO DO SERVIÇO SIGNALR
            ISubscriptionService subscriptionService
            
        )
        {
            _context = context;
            _emailSender = emailSender;
            _razorRenderer = razorRenderer;
            _logger = logger;
            _mercadoPagoService = mercadoPagoService;
            _refundNotification = refundNotification;
            _subscriptionService = subscriptionService;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Este método foi projetado para ser executado por um job em segundo plano (ex: Hangfire).
        /// Ele relança exceções para permitir que o sistema de jobs trate falhas e execute novas tentativas.
        /// </remarks>
        // Assinatura simplificada, sem o userId redundante
        public async Task VerifyAndProcessNotificationAsync(string internalPaymentId)
        {
            _logger.LogInformation(
                "Iniciando processamento de notificação para PaymentId: {PaymentId}",
                internalPaymentId
            );

            var localPayment = await SearchForPaymentAsync(internalPaymentId);
            if (localPayment == null)
                throw new ResourceNotFoundException($"Pagamento com ID {internalPaymentId} não foi encontrado.");

            var user = localPayment.User;
            if (user == null)
                throw new ResourceNotFoundException(
                    $"Usuário associado ao pagamento {internalPaymentId} não foi encontrado.");

            // 1. Busca o status mais recente do pagamento no Mercado Pago
            var externPayment = await _mercadoPagoService.GetPaymentStatusAsync(localPayment.ExternalId);
            if (externPayment == null)
            {
                _logger.LogWarning("Não foi possível obter detalhes do pagamento externo {ExternalId}",
                    localPayment.ExternalId);
                // Você pode querer lançar uma exceção aqui para que o Hangfire tente novamente.
                throw new Exception($"Falha ao obter detalhes do pagamento {localPayment.ExternalId} do Mercado Pago.");
            }

            // 2. A LÓGICA PRINCIPAL COMEÇA AQUI
            if (externPayment.Status == "approved")
            {
                // 3. Verifica se a assinatura JÁ EXISTE.
                if (localPayment.Subscription == null)
                {
                    // 3a. SE NÃO EXISTE, é um pagamento único (PIX ou Cartão)! VAMOS CRIAR A ASSINATURA.
                    _logger.LogInformation(
                        "Pagamento {PaymentId} aprovado. Nenhuma assinatura local encontrada, criando uma nova...",
                        internalPaymentId);

                    // Extrai os metadados que você salvou na criação do pagamento
                    var metadata = JsonSerializer.Deserialize<PaymentMetadata>(externPayment.ExternalReference);
                    if (metadata == null || metadata.PlanPublicId == Guid.Empty)
                    {
                        throw new InvalidOperationException(
                            $"Metadados (ExternalReference) inválidos ou ausentes no pagamento {externPayment.Id}. Não é possível criar a assinatura.");
                    }

                    // CHAMA O SERVIÇO ESPECIALIZADO PARA CRIAR A ASSINATURA NO BANCO
                    // (Este é o método ActivateSubscriptionFromSinglePaymentAsync que desenhamos antes,
                    // que deve estar em um ISubscriptionService injetado nesta classe)
                    await _subscriptionService.ActivateSubscriptionFromSinglePaymentAsync(
                        user.Id,
                        metadata.PlanPublicId,
                        externPayment.Id.ToString(),
                        externPayment.Payer.Email,
                        localPayment.LastFourDigits
                    );

                    _logger.LogInformation("Assinatura de pagamento único criada com sucesso para o usuário {UserId}.",
                        user.Id);
                }
                else
                {
                    // 3b. SE JÁ EXISTE, é um pagamento de uma assinatura recorrente. Apenas atualizamos o status.
                    _logger.LogInformation(
                        "Pagamento {PaymentId} aprovado. Atualizando status da assinatura existente {SubscriptionId}.",
                        internalPaymentId, localPayment.Subscription.Id);
                    localPayment.Subscription.Status = "active";
                }

                // Atualiza o status do pagamento local para 'approved'
                localPayment.Status = "approved";
                await _context.SaveChangesAsync();
                await SendConfirmationEmailAsync(user, internalPaymentId);
            }
            else if (new[] { "rejected", "cancelled", "refunded" }.Contains(externPayment.Status))
            {
                // Lógica para outros status (rejeitado, cancelado, etc.)
                localPayment.Status = externPayment.Status;
                if (localPayment.Subscription != null)
                {
                    localPayment.Subscription.Status = externPayment.Status;
                }

                await _context.SaveChangesAsync();

                if (externPayment.Status == "refunded")
                {
                    await _refundNotification.SendRefundStatusUpdate(localPayment.UserId, "completed",
                        "Seu reembolso foi processado com sucesso!");
                    await SendRefundConfirmationEmailAsync(user, internalPaymentId);
                }
                else
                {
                    await SendRejectionEmailAsync(user, internalPaymentId);
                }
            }
        }


        /// <summary>
            /// Busca o status de um pagamento no banco de dados local.
            /// </summary>
            /// <param name="paymentId">O ID do pagamento a ser consultado.</param>
            /// <returns>A string representando o status do pagamento, ou nulo se não encontrado.</returns>
            /// <exception cref="ArgumentException">Lançada se o <paramref name="paymentId"/> não for um GUID válido.</exception>
            /// <exception cref="AppServiceException">Lançada se ocorrer um erro ao acessar o banco de dados.</exception>
            private async Task<Models.Payments?> SearchForPaymentAsync(string paymentId)
            {
                try
                {
                    var payment = await _context
                        .Payments.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == paymentId);

                    return payment;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro ao buscar o status do pagamento {PaymentId} no banco de dados.",
                        paymentId
                    );
                    throw new AppServiceException($"Falha ao consultar o pagamento {paymentId}.", ex);
                }
            }

            /// <summary>
            /// Renderiza e envia um e-mail de confirmação de pagamento para o usuário.
            /// </summary>
            /// <param name="user">O usuário que receberá o e-mail.</param>
            /// <param name="paymentId">O ID do pagamento confirmado.</param>
            /// <exception cref="ExternalApiException">Lançada se houver uma falha ao renderizar o template ou enviar o e-mail.</exception>
            private async Task SendConfirmationEmailAsync(Users user, string paymentId)
            {
                try
                {
                    var subject = "Seu pagamento foi aprovado! 🎉";
                    var viewModel = new ConfirmationEmailViewModel
                    {
                        UserName = user.Name,
                        PaymentId = paymentId,
                    };
                    var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                        "~/Pages/EmailTemplates/Confirmation/Email.cshtml",
                        viewModel
                    );
                    var plainTextBody =
                        $"Olá, {viewModel.UserName}! Seu pagamento com ID {viewModel.PaymentId} foi aprovado com sucesso.";

                    await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                    _logger.LogInformation(
                        "E-mail de confirmação enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                        user.Email,
                        paymentId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Falha ao enviar e-mail de CONFIRMAÇÃO para {UserEmail} (PaymentId: {PaymentId}).",
                        user.Email,
                        paymentId
                    );
                    // Lança uma exceção específica de API externa para sinalizar o tipo de erro
                    throw new ExternalApiException(
                        "Falha ao renderizar ou enviar o e-mail de confirmação.",
                        ex
                    );
                }
            }

            // Repetimos o mesmo padrão para o e-mail de rejeição
            /// <summary>
            /// Renderiza e envia um e-mail de rejeição de pagamento para o usuário.
            /// </summary>
            /// <param name="user">O usuário que receberá o e-mail.</param>
            /// <param name="paymentId">O ID do pagamento rejeitado.</param>
            /// <exception cref="ExternalApiException">Lançada se houver uma falha ao renderizar o template ou enviar o e-mail.</exception>
            private async Task SendRejectionEmailAsync(Users user, string paymentId)
            {
                try
                {
                    var subject = "Atenção: Ocorreu um problema com seu pagamento";
                    var viewModel = new RefundConfirmationEmailViewModel
                    {
                        UserName = user.Name,
                        PaymentId = paymentId,
                        ConfirmationDate = DateTime.UtcNow, // Use a data atual
                        AccountUrl = "https://seusite.com/minha-conta", // Coloque a URL real aqui
                    };
                    var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                        "~/Pages/EmailTemplates/Rejection/Email.cshtml",
                        viewModel
                    );
                    var plainTextBody =
                        $"Olá, {user.Name}. Infelizmente, ocorreu um problema com o seu pagamento de ID {paymentId} e ele foi rejeitado.";

                    await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                    _logger.LogInformation(
                        "E-mail de rejeição enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                        user.Email,
                        paymentId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Falha ao enviar e-mail de REJEIÇÃO para {UserEmail} (PaymentId: {PaymentId}).",
                        user.Email,
                        paymentId
                    );
                    throw new ExternalApiException(
                        "Falha ao renderizar ou enviar o e-mail de rejeição.",
                        ex
                    );
                }
            }

            /// <summary>
            /// Renderiza e envia um e-mail de confirmação do reembolso do pagamento para o usuário.
            /// </summary>
            /// <param name="user">O usuário que receberá o e-mail.</param>
            /// <param name="paymentId">O ID do pagamento confirmado.</param>
            /// <exception cref="ExternalApiException">Lançada se houver uma falha ao renderizar o template ou enviar o e-mail.</exception>
            private async Task SendRefundConfirmationEmailAsync(Users user, string paymentId)
            {
                try
                {
                    var subject = "Seu Reembolso foi aprovado! 🎉";
                    var viewModel = new ConfirmationEmailViewModel
                    {
                        UserName = user.Name,
                        PaymentId = paymentId,
                    };
                    var htmlBody = await _razorRenderer.RenderViewToStringAsync(
                        "~/Pages/EmailTemplates/Refund/Email.cshtml",
                        viewModel
                    );
                    var plainTextBody =
                        $"Olá, {viewModel.UserName}! Seu pagamento com ID {viewModel.PaymentId} foi Reembolsado com sucesso.";

                    await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, plainTextBody);
                    _logger.LogInformation(
                        "E-mail de confirmação de reembolso enviado com sucesso para {UserEmail} referente ao pagamento {PaymentId}.",
                        user.Email,
                        paymentId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Falha ao enviar e-mail de CONFIRMAÇÃO para {UserEmail} (PaymentId: {PaymentId}).",
                        user.Email,
                        paymentId
                    );
                    // Lança uma exceção específica de API externa para sinalizar o tipo de erro
                    throw new ExternalApiException(
                        "Falha ao renderizar ou enviar o e-mail de confirmação.",
                        ex
                    );
                }
            }
        }
    }