using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Caching.Record;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record; // Nossas exceções
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Utils;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    /// <summary>
    /// Implementa <see cref="ICreditCardPaymentService"/> para processar pagamentos com cartão de crédito.
    /// </summary>
    public class CreditCardPaymentService : ICreditCardPaymentService
    {
        private const string IDEMPOTENCY_PREFIX = "CreditCardPayment";
        private readonly ApiDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreditCardPaymentService> _logger;
        private readonly IPaymentNotificationService _notificationService;
        private readonly GeneralSettings _generalSettings;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;
        private readonly IClientService _clientService;

        public CreditCardPaymentService(
            ApiDbContext context,
            ISubscriptionService subscriptionService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreditCardPaymentService> logger,
            IPaymentNotificationService notificationService,
            IOptions<GeneralSettings> generalSettings,
            ICacheService cacheService,
            IUserContext userContext,
            IClientService clientService
        )
        {
            if (userContext == null) throw new ArgumentNullException(nameof(userContext));
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _notificationService = notificationService;
            _generalSettings = generalSettings.Value;
            _cacheService = cacheService;
            _userContext = userContext;
            _clientService = clientService;
        }

        /// <inheritdoc />
        /// <summary>
        /// Cria um pagamento ou assinatura com base na solicitação.
        /// </summary>
        /// <param name="request">Os dados da solicitação de pagamento.</param>
        public async Task<CachedResponse> CreatePaymentOrSubscriptionAsync(
            CreditCardPaymentRequestDto request,
            string idempotencyKey // 1. A assinatura do método agora inclui a idempotencyKey
        )
        {
            // Validação "Fail-Fast"
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Os dados do pagamento não podem ser nulos.");

            var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_{idempotencyKey}";

            // 2. A lógica de cache envolve toda a operação, garantindo a idempotência
            var response = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    // 3. Tratamento de erro para cachear falhas e garantir que não sejam reprocessadas
                    try
                    {
                        object result;

                        // 4. A lógica de negócio original está dentro da "factory"
                        if (string.Equals(request.Plano, "anual", StringComparison.OrdinalIgnoreCase))
                        {
                            result = await CreateSubscriptionInternalAsync(request);
                        }
                        else
                        {
                            result = await CreateSinglePaymentInternalAsync(request);
                        }

                        // 5. O retorno de sucesso é padronizado para CachedResponse
                        return new CachedResponse(result, 201); // 201 Created
                    }
                    catch (MercadoPagoApiException e)
                    {
                        var errorBody = new { error = "MercadoPago Error", message = e.ApiError.Message };
                        return new CachedResponse(errorBody, 400); // 400 Bad Request
                    }
                    catch (Exception ex)
                    {
                        var errorBody = new { message = "Ocorreu um erro inesperado.", error = ex.Message };
                        return new CachedResponse(errorBody, 500); // 500 Internal Server Error
                    }
                },
                TimeSpan.FromHours(24)
            );

            return response;
        }

        /// <summary>
        /// Cria um pagamento único com base nos dados fornecidos.
        /// </summary>
        /// <param name="creditCardPaymentData">Os dados do pagamento.</param>
        /// <returns>Um objeto representando a resposta do pagamento.</returns>
        /// <exception cref="ArgumentException">Se os dados do pagamento forem inválidos.</exception>
        /// <exception cref="AppServiceException">Se ocorrer um erro ao criar o pagamento.</exception>
        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(
            CreditCardPaymentRequestDto paymentData
        )
        {
            var userId = _userContext.GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (
                paymentData.Payer?.Email is null
                || paymentData.Payer.Identification?.Number is null
            )
            {
                throw new ArgumentException("Dados do pagador (email, CPF) são obrigatórios.");
            }

            string customerId = user.MercadoPagoCustomerId;
            if (string.IsNullOrEmpty(customerId))
            {
                var newCustomer = await _clientService.CreateCustomerAsync(user.Email, user.Name);
                customerId = newCustomer.Id;
                user.MercadoPagoCustomerId = customerId;
            }

            var savedCard = await _clientService.AddCardToCustomerAsync(paymentData.Token);
            string lastFourDigitsDaFonteSegura = savedCard.LastFourDigits;

            await _notificationService.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate("A processar o seu pagamento...", "processing", false)
            );

            var novoPagamento = new Models.Payments
            {
                UserId = userId,
                Status = "iniciando",
                PayerEmail = paymentData.Payer.Email,
                Method = paymentData.PaymentMethodId,
                CustomerCpf = paymentData.Payer.Identification.Number,
                Amount = paymentData.Amount,
                Installments = paymentData.Installments,
                ExternalId = Guid.NewGuid().ToString(), 
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

                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = paymentData.Amount,
                    Token = paymentData.Token,
                    Description = "Pagamento do curso - " + userId, // Descrição clara para o cliente
                    Installments = paymentData.Installments,
                    PaymentMethodId = paymentData.PaymentMethodId,
                    IssuerId = paymentData.IssuerId,
                    Payer = new PaymentPayerRequest
                    {
                        Email = paymentData.Payer.Email,
                        Identification = new IdentificationRequest
                        {
                            Type = paymentData.Payer.Identification.Type,
                            Number = paymentData.Payer.Identification.Number,
                        },
                    },
                    ExternalReference = novoPagamento.ExternalId, // Referência externa para rastreamento
                    NotificationUrl = $"{_generalSettings.BaseUrl}/webhook/mercadopago"
                };

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
                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status = PaymentStatusMapper.MapFromMercadoPago(payment.Status);
                novoPagamento.DateApproved = payment.DateApproved;
                novoPagamento.UpdatedAt = DateTime.UtcNow;
                novoPagamento.LastFourDigits = lastFourDigitsDaFonteSegura;

                await _context.SaveChangesAsync(); // O segundo e final save
                _logger.LogInformation(
                    "Pagamento {PaymentId} processado e atualizado no banco. Status: {Status}",
                    novoPagamento.PaymentId,
                    novoPagamento.Status
                );

                return new PaymentResponseDto(
                    payment.Status,
                    payment.Id,
                    null,
                    "Pagamento processado.",
                    null,
                    null);
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
            var userIdString = _userContext.GetCurrentUserId();

            try
            {
                await _notificationService.SendStatusUpdateAsync(
                    userIdString,
                    new PaymentStatusUpdate("Validando dados da assinatura...", "processing", false)
                );

                if (string.IsNullOrEmpty(subscriptionData.PlanExternalId
                        .ToString())) // Supondo que o DTO foi atualizado
                {
                    throw new ArgumentException("O ID externo do plano é obrigatório.");
                }

                // Apenas montamos o DTO para o serviço.
                var createSubscriptionDto = new CreateSubscriptionDto
                (
                    subscriptionData.PlanExternalId.ToString(),
                    subscriptionData?.Payer?.Email,
                    subscriptionData?.Token,
                    $"{_generalSettings.BaseUrl}/Subscription/Success", // O nome pode ser obtido depois ou não ser necessário aqui
                    "Nome do Plano"
                );

                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null)
                {
                    throw new UnauthorizedAccessException("Contexto do usuário não encontrado.");
                }

                await _notificationService.SendStatusUpdateAsync(
                    userIdString,
                    new PaymentStatusUpdate("Criando sua assinatura...", "processing", false)
                );

                // A chamada ao serviço agora retorna a nossa entidade 'Subscription' completa.
                var createdSubscription =
                    await _subscriptionService.CreateSubscriptionAndCustomerIfNeededAsync(
                        createSubscriptionDto
                    );

                // LÓGICA REMOVIDA: Toda a criação e salvamento do 'new Subscription' foi retirada daqui.
                // A responsabilidade agora é 100% do serviço.

                _logger.LogInformation(
                    "Fluxo de criação de assinatura concluído para o usuário {UserId}. ID da Assinatura: {SubscriptionId}",
                    userIdString,
                    createdSubscription.Id
                );

                // A notificação de sucesso agora pode usar os dados da assinatura que o serviço retornou.
                await _notificationService.SendStatusUpdateAsync(
                    userIdString,
                    new PaymentStatusUpdate(
                        "Assinatura criada com sucesso!",
                        "approved",
                        true,
                        createdSubscription.PaymentId,
                        createdSubscription.ExternalId
                    )
                );

                return new { Id = createdSubscription.ExternalId, Status = createdSubscription.Status };
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
                    userIdString
                );
                throw new AppServiceException(
                    "Ocorreu um erro inesperado em nosso sistema ao criar a assinatura.",
                    ex
                );
            }
        }
    }
}