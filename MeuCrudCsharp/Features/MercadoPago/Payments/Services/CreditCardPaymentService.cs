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
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record; // Nossas exceções
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Utils;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
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
        private readonly ILogger<CreditCardPaymentService> _logger;
        private readonly IPaymentNotificationService _notificationService;
        private readonly GeneralSettings _generalSettings;
        private readonly ICacheService _cacheService;
        private readonly IUserContext _userContext;
        private readonly IClientService _clientService;
        private readonly IUserRepository _userRepository;

        public CreditCardPaymentService(
            ApiDbContext context,
            ISubscriptionService subscriptionService,
            ILogger<CreditCardPaymentService> logger,
            IPaymentNotificationService notificationService,
            IOptions<GeneralSettings> generalSettings,
            ICacheService cacheService,
            IUserContext userContext,
            IClientService clientService,
            IUserRepository userRepository
        )
        {
            if (userContext == null) throw new ArgumentNullException(nameof(userContext));
            _context = context;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _notificationService = notificationService;
            _generalSettings = generalSettings.Value;
            _cacheService = cacheService;
            _userContext = userContext;
            _clientService = clientService;
            _userRepository = userRepository;
        }


        public async Task<CachedResponse> CreatePaymentOrSubscriptionAsync(
            CreditCardPaymentRequestDto request,
            string idempotencyKey
        )
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Os dados do pagamento não podem ser nulos.");

            var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_{idempotencyKey}";

            var response = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        object result;

                        if (string.Equals(request.Plano, "anual", StringComparison.OrdinalIgnoreCase))
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
                        var errorBody = new { error = "MercadoPago Error", message = e.ApiError.Message };
                        return new CachedResponse(errorBody, 400);
                    }
                    catch (Exception ex)
                    {
                        var errorBody = new { message = "Ocorreu um erro inesperado.", error = ex.Message };
                        return new CachedResponse(errorBody, 500);
                    }
                },
                TimeSpan.FromHours(24)
            );

            return response;
        }


        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(
            CreditCardPaymentRequestDto paymentData
        )
        {
            var userId = await _userContext.GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId);
            Models.Payments novoPagamento = null;

            if (paymentData.Payer?.Email is null || paymentData.Payer.Identification?.Number is null)
            {
                throw new ArgumentException("Dados do pagador (email, CPF) são obrigatórios.");
            }

            try
            {
                if (string.IsNullOrEmpty(user.MercadoPagoCustomerId))
                {
                    _logger.LogInformation("Usuário {UserId} não possui CustomerId. Criando novo cliente e cartão.",
                        userId);
                    var customerWithCard = await _clientService.CreateCustomerWithCardAsync(
                        user.Email,
                        user.Name,
                        paymentData.Token
                    );
                    user.MercadoPagoCustomerId = customerWithCard.CustomerId;
                }
                else
                {
                    _logger.LogInformation("Usuário {UserId} já possui CustomerId. Adicionando novo cartão.", userId);
                    await _clientService.AddCardToCustomerAsync(user.MercadoPagoCustomerId,paymentData.Token);
                }

                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate("A processar o seu pagamento...", "processing", false)
                );

                novoPagamento = new Models.Payments()
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

                _context.Payments.Add(novoPagamento);
                _logger.LogInformation(
                    "Registro de pagamento inicial criado com ID: {PaymentId}",
                    novoPagamento.Id
                );

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
                            payment.Id.ToString()
                        )
                    );
                }
                else
                {
                    await _notificationService.SendStatusUpdateAsync(
                        userId,
                        new PaymentStatusUpdate(
                            payment.StatusDetail ?? "O pagamento foi recusado.",
                            "failed",
                            true,
                            payment.Id.ToString()
                        )
                    );
                }

                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status = PaymentStatusMapper.MapFromMercadoPago(payment.Status);
                novoPagamento.DateApproved = payment.DateApproved;
                novoPagamento.UpdatedAt = DateTime.UtcNow;
                novoPagamento.LastFourDigits = payment.Card.LastFourDigits;

                await _context.SaveChangesAsync();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar pagamento via MP para o usuário {UserId}.", userId);

                var errorMessage = (ex is MercadoPagoApiException mpex)
                    ? mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor."
                    : "Ocorreu um erro inesperado em nosso sistema.";

                await _notificationService.SendStatusUpdateAsync(
                    userId, new PaymentStatusUpdate(errorMessage, "error", true)
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
            if (user == null) throw new AppServiceException("Usuário não encontrado.");

            await _notificationService.SendStatusUpdateAsync(userId,
                new PaymentStatusUpdate("Validando seus dados...", "processing", false));

            try
            {
                CustomerWithCardResponseDto customerWithCard = null;

                if (string.IsNullOrEmpty(user.MercadoPagoCustomerId))
                {
                    _logger.LogInformation("Usuário {UserId} não possui CustomerId. Criando novo cliente e cartão.",
                        userId);
                    customerWithCard = await _clientService.CreateCustomerWithCardAsync(
                        user.Email,
                        user.Name,
                        subscriptionData.Token
                    );
                    user.MercadoPagoCustomerId = customerWithCard.CustomerId;
                }
                else
                {
                    _logger.LogInformation("Usuário {UserId} já possui CustomerId. Adicionando novo cartão.", userId);
                    var card = await _clientService.AddCardToCustomerAsync(user.MercadoPagoCustomerId,subscriptionData.Token);
                    customerWithCard =
                        new CustomerWithCardResponseDto(
                            user.MercadoPagoCustomerId,
                            user.Email,
                            new CardInCustomerResponseDto(
                                card.Id,
                                card.LastFourDigits,
                                card.ExpirationMonth,
                                card.ExpirationYear)
                        );
                }

                await _notificationService.SendStatusUpdateAsync(
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
                
                await _userRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "Fluxo de criação de assinatura concluído para o usuário {UserId}. ID da Assinatura: {SubscriptionId}",
                    userId,
                    createdSubscription.Id
                );

                await _notificationService.SendStatusUpdateAsync(
                    userId,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no fluxo de criação de assinatura para o usuário {UserId}.", userId);

                await _notificationService.SendStatusUpdateAsync(
                    userId, new PaymentStatusUpdate(ex.Message, "error", true)
                );

                throw;
            }
        }
    }
}