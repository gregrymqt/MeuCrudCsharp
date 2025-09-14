using System.Security.Claims;
using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.User;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    /// <summary>
    /// Implementa <see cref="ICreditCardPaymentService"/> para processar pagamentos com cartão de crédito.
    /// </summary>
    public class CreditCardPaymentService : ICreditCardPaymentService
    {
        private readonly ApiDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreditCardPaymentService> _logger;
        private readonly IPaymentNotificationService _notificationService;
        private readonly MercadoPagoSettings _mercadoPagoSettings;
        private readonly GeneralSettings _generalSettings;

        private readonly Dictionary<string, string> _statusMap = new()
        {
            { "approved", "aprovado" },
            { "pending", "pendente" },
            { "in_process", "pendente" },
            { "rejected", "recusado" },
            { "refunded", "reembolsado" },
            { "cancelled", "cancelado" },
        };

        public CreditCardPaymentService(
            ApiDbContext context,
            ISubscriptionService subscriptionService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreditCardPaymentService> logger,
            IPaymentNotificationService notificationService,
            IOptions<MercadoPagoSettings> mercadoPagoSettings,
            IOptions<GeneralSettings> generalSettings
        )
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _notificationService = notificationService;
            _mercadoPagoSettings = mercadoPagoSettings.Value;
            _generalSettings = generalSettings.Value;
        }

        /// <inheritdoc />
        /// <summary>
        /// Cria um pagamento ou assinatura com base na solicitação.
        /// </summary>
        /// <param name="request">Os dados da solicitação de pagamento.</param>
        public async Task<object> CreatePaymentOrSubscriptionAsync(CreditCardPaymentRequestDto request)
        {
            // MUDANÇA 2: Validação "Fail-Fast"
            if (request == null)
                throw new ArgumentNullException(
                    nameof(request),
                    "Os dados do pagamento não podem ser nulos."
                );

            if (string.Equals(request.Plano, "anual", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateSubscriptionInternalAsync(request);
            }

            return await CreateSinglePaymentInternalAsync(request);
        }

        /// <summary>
        /// Cria um pagamento único com base nos dados fornecidos.
        /// </summary>
        /// <param name="creditCardPaymentData">Os dados do pagamento.</param>
        /// <returns>Um objeto representando a resposta do pagamento.</returns>
        /// <exception cref="ArgumentException">Se os dados do pagamento forem inválidos.</exception>
        /// <exception cref="AppServiceException">Se ocorrer um erro ao criar o pagamento.</exception>
        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(
            CreditCardPaymentRequestDto creditCardPaymentData
        )
        {
            var userId = GetCurrentUserId();

            if (
                creditCardPaymentData.Payer?.Email is null
                || creditCardPaymentData.Payer.Identification?.Number is null
            )
            {
                throw new ArgumentException("Dados do pagador (email, CPF) são obrigatórios.");
            }

            await _notificationService.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate("A processar o seu pagamento...", "processing", false)
            );

            var novoPagamento = new Models.Payments
            {
                UserId = userId,
                Status = "iniciando",
                PayerEmail = creditCardPaymentData.Payer.Email,
                Method = creditCardPaymentData.PaymentMethodId,
                CustomerCpf = creditCardPaymentData.Payer.Identification.Number,
                Amount = creditCardPaymentData.Amount,
                Installments = creditCardPaymentData.Installments,
                ExternalId = Guid.NewGuid().ToString(), // Usando um ID externo único para idempotência
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Salva o registro inicial no banco
            _context.Payments.Add(novoPagamento);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Registro de pagamento inicial criado com ID: {PaymentId}",
                novoPagamento.Id
            );

            try
            {
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        "Comunicando com o provedor de pagamento...",
                        "processing",
                        false
                    )
                );

                var paymentClient = new PaymentClient();
                var requestOptions = new RequestOptions
                {
                    CustomHeaders = { { "X-Idempotency-Key", novoPagamento.ExternalId } },
                };

                // =======================================================
                //  INÍCIO DA CORREÇÃO PRINCIPAL
                // =======================================================
                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = creditCardPaymentData.Amount,
                    Token = creditCardPaymentData.Token,
                    Description = "Pagamento do curso - " + userId, // Descrição clara para o cliente
                    Installments = creditCardPaymentData.Installments,
                    PaymentMethodId = creditCardPaymentData.PaymentMethodId,
                    IssuerId = creditCardPaymentData.IssuerId,
                    Payer = new PaymentPayerRequest
                    {
                        Email = creditCardPaymentData.Payer.Email,
                        Identification = new IdentificationRequest
                        {
                            Type = creditCardPaymentData.Payer.Identification.Type,
                            Number = creditCardPaymentData.Payer.Identification.Number,
                        },
                    },
                    ExternalReference = novoPagamento.ExternalId, // Referência externa para rastreamento
                    NotificationUrl = $"{_generalSettings.BaseUrl}/webhook/mercadopago"
                };
                // =======================================================
                //  FIM DA CORREÇÃO PRINCIPAL
                // =======================================================

                Payment payment = await paymentClient.CreateAsync(paymentRequest, requestOptions);

                if (payment.Status == "approved" || payment.Status == "in_process")
                {
                    await _notificationService.SendStatusUpdateAsync(
                        userId,
                        new PaymentStatusUpdate(
                            "Pagamento aprovado com sucesso!",
                            "approved",
                            true,
                            payment.Id.ToString() // <-- Enviando o ID para o front-end
                        )
                    );
                }
                else // Se foi 'rejected', 'cancelled', ou qualquer outro status de falha
                {
                    await _notificationService.SendStatusUpdateAsync(
                        userId,
                        new PaymentStatusUpdate(
                            payment.StatusDetail ?? "O pagamento foi recusado.",
                            "failed",
                            true,
                            payment.Id.ToString() // <-- Enviando o ID para o front-end
                        )
                    );
                }

                // Atualiza o registro no banco com os dados retornados pela API
                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status =
                    MapPaymentStatus(payment.Status); // Supondo que você tenha este método de mapeamento
                novoPagamento.DateApproved = payment.DateApproved;
                novoPagamento.UpdatedAt = DateTime.UtcNow;

                // **MELHORIA DE SEGURANÇA**: Evita erro se 'Card' ou 'LastFourDigits' for nulo.
                if (int.TryParse(payment.Card?.LastFourDigits, out int lastFourDigits))
                {
                    novoPagamento.LastFourDigits = lastFourDigits;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Pagamento {PaymentId} processado e atualizado no banco. Status: {Status}",
                    novoPagamento.PaymentId,
                    novoPagamento.Status
                );

                return new PaymentResponseDto
                {
                    Id = payment.Id,
                    Status = payment.Status,
                    Message = "Pagamento processado.",
                };
            }
            catch (MercadoPagoApiException mpex)
            {
                novoPagamento.Status = "falhou";
                await _context.SaveChangesAsync();
                _logger.LogError(
                    mpex,
                    "Erro da API do Mercado Pago: {ApiError}",
                    mpex.ApiError.Message
                );

                // CORREÇÃO: Notifica o front-end sobre o erro ANTES de lançar a exceção
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor.",
                        "error",
                        true
                    )
                );

                throw new ExternalApiException(
                    mpex.ApiError?.Message ?? "Ocorreu um erro ao processar seu pagamento.",
                    mpex
                );
            }
            catch (Exception ex)
            {
                novoPagamento.Status = "erro_interno";
                await _context.SaveChangesAsync();
                _logger.LogError(
                    ex,
                    "Erro inesperado ao processar pagamento para {UserId}.",
                    userId
                );

                // CORREÇÃO: Notifica o front-end sobre o erro ANTES de lançar a exceção
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        "Ocorreu um erro inesperado em nosso sistema.",
                        "error",
                        true
                    )
                );

                throw new AppServiceException("Ocorreu um erro inesperado em nosso sistema.", ex);
            }
        }

        /// <summary>
        /// Cria uma nova assinatura com base nos dados fornecidos.
        /// </summary>
        /// <param name="subscriptionData">Os dados da assinatura.</param>
        /// <returns>Um objeto representando a resposta da criação da assinatura.</returns>
        /// <exception cref="ArgumentException">Se os dados da assinatura forem inválidos.</exception>
        /// <exception cref="AppServiceException">Se ocorrer um erro ao criar a assinatura.</exception>
        private async Task<object> CreateSubscriptionInternalAsync(
            CreditCardPaymentRequestDto subscriptionData
        )
        {
            var userId = GetCurrentUserId();

            // MUDANÇA 3: Bloco try-catch robusto
            try
            {
                // 1. Notificação inicial para o front-end
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate("Validando dados da assinatura...", "processing", false)
                );

                // 1. Tente obter a configuração do plano de forma segura
                if (!_mercadoPagoSettings.Plans.TryGetValue("Anual", out PlanDetail planDetail))
                {
                    throw new InvalidOperationException(
                        "A configuração para o plano 'Anual' não foi encontrada. Verifique o appsettings."
                    );
                }

                // 3. A partir daqui, o compilador e você sabem que 'planDetail' NÃO É NULO.
                // O resto do seu código pode continuar exatamente como estava.
                var plan = await _context
                    .Plans.AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.ExternalPlanId == planDetail.Id
                    );

                if (plan == null)
                {
                    throw new ResourceNotFoundException(
                        $"O plano com ID externo '{planDetail.Id}' não foi encontrado no banco de dados."
                    );
                }

                var createSubscriptionDto = new CreateSubscriptionDto
                {
                    PreapprovalPlanId = planDetail.Id, // Agora 100% seguro
                    PayerEmail = subscriptionData?.Payer?.Email,
                    CardTokenId = subscriptionData?.Token,
                    Reason = plan.Name,
                    BackUrl = $"{_generalSettings.BaseUrl}/Subscription/Success",
                };

                var user = _httpContextAccessor.HttpContext?.User;

                if (user == null)
                {
                    throw new UnauthorizedAccessException(
                        "Não foi possível obter o contexto do usuário para criar a assinatura."
                    );
                }

                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate("Criando sua assinatura...", "processing", false)
                );

                var subscriptionResponse =
                    await _subscriptionService.CreateSubscriptionAndCustomerIfNeededAsync(
                        createSubscriptionDto,
                        user
                    );

                var novaAssinatura = new Subscription
                {
                    UserId = userId,
                    PlanId = plan.Id,
                    ExternalId = subscriptionResponse.Id,
                    Status = MapPaymentStatus(subscriptionResponse.Status), // Supondo que você tenha este método
                    PayerEmail = subscriptionData?.Payer?.Email,
                    UpdatedAt = DateTime.UtcNow,
                    PaymentId = subscriptionResponse.Id,
                };

                _context.Subscriptions.Add(novaAssinatura);
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Assinatura {SubscriptionId} criada para o usuário {UserId}.",
                    novaAssinatura.ExternalId,
                    userId
                );

                // 2. Notificação final de sucesso
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        "Assinatura criada com sucesso!",
                        "approved",
                        true,
                        novaAssinatura.PaymentId,
                        novaAssinatura.ExternalId // Enviando o ID da assinatura
                    )
                );

                return new { subscriptionResponse.Id, subscriptionResponse.Status };
            }
            catch (ResourceNotFoundException) // Deixa a exceção específica passar para o controller
            {
                throw;
            }
            catch (Exception ex) // Captura qualquer outro erro (da API do MP ou do banco)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar assinatura para o usuário {UserId}.",
                    userId
                );
                throw new AppServiceException(
                    "Ocorreu um erro inesperado em nosso sistema ao criar a assinatura.",
                    ex
                );
            }
        }

        public string MapPaymentStatus(string mercadopagoStatus)
        {
            return _statusMap.TryGetValue(mercadopagoStatus, out var status) ? status : "pendente";
        }

        private string GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );
            if (string.IsNullOrEmpty(userIdString))
            {
                throw new AppServiceException(
                    "A identificação do usuário não pôde ser encontrada na sessão atual."
                );
            }

            return userIdString;
        }
    }
}