using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;

using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Caching.Record;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Hub;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Utils;
using MeuCrudCsharp.Features.Shared.Work;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services;

public class PixPaymentService : IPixPaymentService
{
    private readonly ILogger<PixPaymentService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IPaymentNotificationHub _notificationHub;

    // REMOVIDO: private readonly ApiDbContext _dbContext;
    // ADICIONADO: Repository e UnitOfWork
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    private readonly GeneralSettings _generalsettings;
    private const string IDEMPOTENCY_PREFIX = "PixPayment";
    private readonly IUserContext _userContext;

    public PixPaymentService(
        ILogger<PixPaymentService> logger,
        ICacheService cacheService,
        IPaymentNotificationHub notificationHub,
        IPaymentRepository paymentRepository, // Injeção do Repo
        IUnitOfWork unitOfWork, // Injeção do UoW
        IOptions<GeneralSettings> settings,
        IUserContext userContext
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _notificationHub = notificationHub;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _generalsettings = settings.Value;
        _userContext = userContext;
    }

    public async Task<CachedResponse> CreateIdempotentPixPaymentAsync(
        CreatePixPaymentRequest request,
        string idempotencyKey
    )
    {
        var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_pix_{idempotencyKey}";

        var response = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                try
                {
                    var result = await CreatePixPaymentAsync(
                        await _userContext.GetCurrentUserId(),
                        request,
                        idempotencyKey
                    );

                    return new CachedResponse(result, 200);
                }
                catch (MercadoPagoApiException mpex)
                {
                    var errorBody = new
                    {
                        error = "MercadoPago Error",
                        message = mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor.",
                    };
                    _logger.LogError(
                        mpex,
                        "Erro da API do Mercado Pago (IdempotencyKey: {Key}): {ApiError}",
                        idempotencyKey,
                        mpex.ApiError?.Message
                    );
                    return new CachedResponse(errorBody, 400);
                }
                catch (Exception ex)
                {
                    var errorBody = new
                    {
                        message = "Ocorreu um erro inesperado.",
                        error = ex.Message,
                    };
                    _logger.LogError(
                        ex,
                        "Erro inesperado ao processar PIX (IdempotencyKey: {Key})",
                        idempotencyKey
                    );
                    return new CachedResponse(errorBody, 500);
                }
            },
            TimeSpan.FromHours(24)
        );

        return response;
    }

    private async Task<PaymentResponseDto> CreatePixPaymentAsync(
        string userId,
        CreatePixPaymentRequest request,
        string externalReference
    )
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("userId is required");
        }

        Models.Payments? novoPixPayment = null;

        try
        {
            await _notificationHub.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate("A processar o seu pagamento...", "processing", false)
            );

            // 1. Prepara a Entidade
            novoPixPayment = new Models.Payments()
            {
                UserId = userId,
                Status = "Iniciando",
                PayerEmail = request.Payer.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExternalId = externalReference,
                Amount = request.TransactionAmount,
                Method = "Pix",
                CustomerCpf = request.Payer.Identification.Number,
            };

            // 2. Persiste Inicialmente (Status "Iniciando") via Repository
            await _paymentRepository.AddAsync(novoPixPayment);
            await _unitOfWork.CommitAsync(); // Gera o ID no Banco

            _logger.LogInformation(
                "Pix payment adicionado com sucesso, com o Id: {Id}",
                novoPixPayment.Id
            );

            await _notificationHub.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate(
                    "Comunicando com o provedor de pagamento...",
                    "processing",
                    false
                )
            );

            // 3. Chama Mercado Pago
            var requestOptions = new RequestOptions
            {
                CustomHeaders = { { "X-Idempotency-Key", externalReference } },
            };

            var paymentClient = new PaymentClient();
            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = request.TransactionAmount,
                Description = request.Description,
                PaymentMethodId = "pix",
                Payer = new PaymentPayerRequest
                {
                    Email = request.Payer.Email,
                    FirstName = request.Payer.FirstName,
                    LastName = request.Payer.LastName,
                    Identification = new IdentificationRequest
                    {
                        Type = request.Payer.Identification.Type,
                        Number = request.Payer.Identification.Number,
                    },
                },
                ExternalReference = externalReference,
                NotificationUrl = $"{_generalsettings.BaseUrl}/webhook/mercadpago",
            };

            Payment payment = await paymentClient.CreateAsync(paymentRequest, requestOptions);

            // 4. Atualiza a Entidade com dados do MP
            novoPixPayment.PaymentId = payment.Id.ToString();
            novoPixPayment.Status = PaymentStatusMapper.MapFromMercadoPago(payment.Status);
            novoPixPayment.DateApproved = payment.DateApproved;
            novoPixPayment.UpdatedAt = DateTime.UtcNow;

            // Usa o Update do Repository + Commit
            _paymentRepository.Update(novoPixPayment);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "Pagamento PIX {PaymentId} para o usuário {UserId} salvo com sucesso.",
                novoPixPayment.PaymentId,
                userId
            );

            // 5. Notifica via Hub
            if (
                payment.Status == "approved"
                || payment.Status == "pending"
                || payment.Status == "in_process"
            )
            {
                await _notificationHub.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(
                        "Pagamento processado com sucesso!",
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

            // 6. Retorno
            return new PaymentResponseDto(
                payment.Status,
                payment.Id.GetValueOrDefault(), // Safe unwrap
                null,
                "Pagamento PIX criado com sucesso.",
                payment.PointOfInteraction?.TransactionData?.QrCode,
                payment.PointOfInteraction?.TransactionData?.QrCodeBase64
            );
        }
        catch (Exception ex)
        {
            var mensagemErro = "Ocorreu um erro inesperado em nosso sistema.";

            if (ex is MercadoPagoApiException mpex)
            {
                mensagemErro = mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor.";
                _logger.LogError(
                    mpex,
                    "Erro da API do Mercado Pago: {ApiError}",
                    mpex.ApiError?.Message
                );
            }
            else
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao processar pagamento para {UserId}.",
                    userId
                );
            }

            // Rollback manual (Remover o registro temporário se falhar)
            if (novoPixPayment != null && novoPixPayment.Id != null)
            {
                _paymentRepository.Remove(novoPixPayment);
                await _unitOfWork.CommitAsync();

                _logger.LogWarning(
                    "Registro de pagamento para o usuário {UserId} foi removido devido a uma falha.",
                    userId
                );
            }

            await _notificationHub.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate(mensagemErro, "error", true)
            );

            throw new AppServiceException(mensagemErro, ex);
        }
    }
}
