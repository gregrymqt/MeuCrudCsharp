using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Caching.Record;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Hub;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Utils;
using MeuCrudCsharp.Features.Shared.Work;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    /// <summary>
    /// Implementa <see cref="ICreditCardPaymentService"/> para processar pagamentos com cartão de crédito.
    /// </summary>
    public class CreditCardPaymentService : ICreditCardPaymentService
    {
        private const string IDEMPOTENCY_PREFIX = "CreditCardPayment";
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<CreditCardPaymentService> _logger;
        private readonly IPaymentNotificationHub _notificationHub;
        private readonly GeneralSettings _generalSettings;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;
        private readonly IClientService _clientService;
        private readonly IUserRepository _userRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreditCardPaymentService(
            ISubscriptionService subscriptionService,
            ILogger<CreditCardPaymentService> logger,
            IPaymentNotificationHub notificationHub,
            IOptions<GeneralSettings> generalSettings,
            ICacheService cacheService,
            IUserContext userContext,
            IClientService clientService,
            IUserRepository userRepository,
            IPaymentRepository paymentRepository,
            IUnitOfWork unitOfWork
        )
        {
            ArgumentNullException.ThrowIfNull(userContext);
            _logger = logger;
            _subscriptionService = subscriptionService;
            _notificationHub = notificationHub;
            _generalSettings = generalSettings.Value;
            _cacheService = cacheService;
            _userContext = userContext;
            _clientService = clientService;
            _userRepository = userRepository;
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CachedResponse> CreatePaymentOrSubscriptionAsync(
            CreditCardPaymentRequestDto request,
            string idempotencyKey
        )
        {
            if (request == null)
                throw new ArgumentNullException(
                    nameof(request),
                    "Os dados do pagamento não podem ser nulos."
                );

            var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_{idempotencyKey}";

            var response = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        object result;

                        if (
                            string.Equals(
                                request.Plano,
                                "anual",
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            result = await CreateSubscriptionInternalAsync(request);
                        }
                        else
                        {
                            result = await CreateSinglePaymentInternalAsync(request);
                        }

                        return new CachedResponse(result, 201);
                    }
                    catch (MercadoPagoApiException e)
                    {
                        var errorBody = new
                        {
                            message = e.ApiError.Message,
                            error = "MercadoPago Error",
                        };
                        return new CachedResponse(errorBody, 400);
                    }
                    catch (Exception ex)
                    {
                        var errorBody = new
                        {
                            message = "Ocorreu um erro inesperado.",
                            error = ex.Message,
                        };
                        return new CachedResponse(errorBody, 500);
                    }
                },
                TimeSpan.FromHours(24)
            );

            return response ?? throw new InvalidOperationException();
        }

        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(
            CreditCardPaymentRequestDto paymentData
        )
        {
            var userId = await _userContext.GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId);

            if (
                paymentData.Payer?.Email is null
                || paymentData.Payer.Identification?.Number is null
            )
            {
                throw new ArgumentException("Dados do pagador (email, CPF) são obrigatórios.");
            }

            try
            {
                // 1. Gerenciar cliente/cartão
                if (string.IsNullOrEmpty(user.CustomerId))
                {
                    _logger.LogInformation(
                        "Usuário {UserId} não possui CustomerId. Criando novo cliente e cartão.",
                        userId
                    );
                    var customerWithCard = await _clientService.CreateCustomerWithCardAsync(
                        user.Email,
                        user.Name,
                        paymentData.Token
                    );
                    user.CustomerId = customerWithCard.CustomerId;
                    _userRepository.Update(user); // Marca para atualização
                }
                else
                {
                    _logger.LogInformation(
                        "Usuário {UserId} já possui CustomerId. Adicionando novo cartão.",
                        userId
                    );
                    await _clientService.AddCardToCustomerAsync(user.CustomerId, paymentData.Token);
                }

                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate("A processar o seu pagamento...", "processing", false)
                );

                // 2. Criar registro inicial de pagamento
                var novoPagamento = new Models.Payments()
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

                await _paymentRepository.AddAsync(novoPagamento); // Usa repository ao invés de _context
                
                _logger.LogInformation(
                    "Registro de pagamento inicial criado com ID: {PaymentId}",
                    novoPagamento.Id
                );

                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        "Comunicando com o provedor de pagamento...",
                        "processing",
                        false
                    )
                );

                // 3. Chamar MercadoPago
                var paymentClient = new PaymentClient();
                var requestOptions = new RequestOptions
                {
                    CustomHeaders = { { "X-Idempotency-Key", novoPagamento.ExternalId } },
                };

                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = paymentData.Amount,
                    Token = paymentData.Token,
                    Description = "Pagamento do curso - " + userId,
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
                    ExternalReference = novoPagamento.ExternalId,
                    NotificationUrl = $"{_generalSettings.BaseUrl}/webhook/mercadopago",
                };

                var payment = await paymentClient.CreateAsync(paymentRequest, requestOptions);

                // 4. Atualizar pagamento com resposta do MercadoPago
                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status = PaymentStatusMapper.MapFromMercadoPago(payment.Status);
                novoPagamento.DateApproved = payment.DateApproved;
                novoPagamento.UpdatedAt = DateTime.UtcNow;
                novoPagamento.LastFourDigits = payment.Card.LastFourDigits;

                _paymentRepository.Update(novoPagamento); // Marca para atualização

                // 5. COMMIT ÚNICO - ATOMICIDADE GARANTIDA
                // Salva todas as alterações (User.CustomerId + Payment) em uma única transação
                await _unitOfWork.CommitAsync();

                // 6. Notificar resultado
                if (payment.Status is "approved" or "in_process")
                {
                    await _notificationHub.SendStatusUpdateAsync(
                        userId,
                        new PaymentStatusUpdate(
                            "Pagamento aprovado com sucesso!",
                            "approved",
                            true,
                            payment.Id.ToString()
                        )
                    );
                }
                else
                {
                    await _notificationHub.SendStatusUpdateAsync(
                        userId,
                        new PaymentStatusUpdate(
                            payment.StatusDetail ?? "O pagamento foi recusado.",
                            "failed",
                            true,
                            payment.Id.ToString()
                        )
                    );
                }

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
                    null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao processar pagamento via MP para o usuário {UserId}.",
                    userId
                );

                var errorMessage =
                    (ex is MercadoPagoApiException mpex)
                        ? mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor."
                        : "Ocorreu um erro inesperado em nosso sistema.";

                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(errorMessage, "error", true)
                );

                if (ex is MercadoPagoApiException mpexForward)
                {
                    throw new ExternalApiException(errorMessage, mpexForward);
                }

                throw new AppServiceException(errorMessage, ex);
            }
        }

        private async Task<object> CreateSubscriptionInternalAsync(
            CreditCardPaymentRequestDto subscriptionData
        )
        {
            var userId = await _userContext.GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new AppServiceException("Usuário não encontrado.");

            await _notificationHub.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate("Validando seus dados...", "processing", false)
            );

            try
            {
                CustomerWithCardResponseDto? customerWithCard;

                if (string.IsNullOrEmpty(user.CustomerId))
                {
                    _logger.LogInformation(
                        "Usuário {UserId} não possui CustomerId. Criando novo cliente e cartão.",
                        userId
                    );
                    customerWithCard = await _clientService.CreateCustomerWithCardAsync(
                        user.Email,
                        user.Name,
                        subscriptionData.Token
                    );
                    user.CustomerId = customerWithCard.CustomerId;
                    _userRepository.Update(user); // Marca para atualização
                }
                else
                {
                    _logger.LogInformation(
                        "Usuário {UserId} já possui CustomerId. Adicionando novo cartão.",
                        userId
                    );
                    var card = await _clientService.AddCardToCustomerAsync(
                        user.CustomerId,
                        subscriptionData.Token
                    );
                    customerWithCard = new CustomerWithCardResponseDto(
                        user.CustomerId,
                        user.Email,
                        new CardInCustomerResponseDto(
                            card.Id,
                            card.LastFourDigits,
                            card.ExpirationMonth,
                            card.ExpirationYear,
                            card.PaymentMethod ?? new PaymentMethodDto("credit_card", "Credit Card")
                        )
                    );
                }

                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate("Criando sua assinatura...", "processing", false)
                );

                var createdSubscription = await _subscriptionService.CreateSubscriptionAsync(
                    userId,
                    subscriptionData.PlanExternalId.ToString(),
                    customerWithCard.Card.Id,
                    subscriptionData.Payer.Email,
                    customerWithCard.Card.LastFourDigits
                );

                // Salva as alterações do usuário (CustomerId) usando o UnitOfWork
                // OBS: A assinatura já foi salva pelo SubscriptionService
                await _unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "Fluxo de criação de assinatura concluído para o usuário {UserId}. ID da Assinatura: {SubscriptionId}",
                    userId,
                    createdSubscription.Id
                );

                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        "Assinatura criada com sucesso!",
                        "approved",
                        true,
                        createdSubscription.PaymentId,
                        createdSubscription.ExternalId
                    )
                );

                return new
                {
                    Id = createdSubscription.ExternalId,
                    Status = createdSubscription.Status,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado no fluxo de criação de assinatura para o usuário {UserId}.",
                    userId
                );

                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(ex.Message, "error", true)
                );

                throw;
            }
        }
    }
}
