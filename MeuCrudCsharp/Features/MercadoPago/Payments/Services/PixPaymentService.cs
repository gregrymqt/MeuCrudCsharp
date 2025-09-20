using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Caching.Record;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Record;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services;

public class PixPaymentService : IPixPaymentService
{
    private readonly ILogger<PixPaymentService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IPaymentNotificationService _notificationService;
    private readonly ApiDbContext _dbContext;
    private readonly GeneralSettings _generalsettings;
    private const string IDEMPOTENCY_PREFIX = "PixPayment";


    public PixPaymentService(ILogger<PixPaymentService> logger,
        ICacheService cacheService,
        IPaymentNotificationService notificationService,
        ApiDbContext dbContext,
        IOptions<GeneralSettings> settings)
    {
        _logger = logger;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _dbContext = dbContext;
        _generalsettings = settings.Value;
    }

    private readonly Dictionary<string, string> _statusMap = new()
    {
        { "approved", "aprovado" },
        { "pending", "pendente" },
        { "in_process", "pendente" },
        { "rejected", "recusado" },
        { "refunded", "reembolsado" },
        { "cancelled", "cancelado" },
    };
    
    public async Task<CachedResponse> CreateIdempotentPixPaymentAsync(string userId, CreatePixPaymentRequest request, string idempotencyKey)
    {
        var cacheKey = $"{IDEMPOTENCY_PREFIX}_idempotency_pix_{idempotencyKey}";

        var response = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                try
                {
                    // Reutilizamos a sua lógica original de criação de PIX aqui dentro.
                    // A chave de idempotência do cliente será usada como referência externa.
                    var result = await this.CreatePixPaymentAsync(userId, request, idempotencyKey);
                
                    // Em caso de sucesso, retornamos o DTO de resposta com status 200 (OK) ou 201 (Created).
                    return new CachedResponse(result, 200); 
                }
                catch (MercadoPagoApiException mpex)
                {
                    var errorBody = new { error = "MercadoPago Error", message = mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor." };
                    _logger.LogError(mpex, "Erro da API do Mercado Pago (IdempotencyKey: {Key}): {ApiError}", idempotencyKey, mpex.ApiError?.Message);
                    return new CachedResponse(errorBody, 400); // Bad Request
                }
                catch (Exception ex)
                {
                    var errorBody = new { message = "Ocorreu um erro inesperado.", error = ex.Message };
                    _logger.LogError(ex, "Erro inesperado ao processar PIX (IdempotencyKey: {Key})", idempotencyKey);
                    return new CachedResponse(errorBody, 500); // Internal Server Error
                }
            },
            TimeSpan.FromHours(24) // Duração do cache de idempotência
        );

        return response;
    }

    private async Task<PaymentResponseDto> CreatePixPaymentAsync(string userId, CreatePixPaymentRequest request, string externalReference)
    {
        if (String.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("userId is required");
        }

        Models.Payments? novoPixPayment = null;

        try
        {
            await _notificationService.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate("A processar o seu pagamento...", "processing", false)
            );

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

            await _dbContext.Payments.AddAsync(novoPixPayment);
            _logger.LogInformation("Pix payment adicionado com sucesso, com o Id:"
                                   + novoPixPayment.Id);

            await _notificationService.SendStatusUpdateAsync(
                userId,
                new PaymentStatusUpdate(
                    "Comunicando com o provedor de pagamento...",
                    "processing",
                    false
                )
            );

            var requestOptions = new RequestOptions
            {
                CustomHeaders = { { "X-Idempotency-Key", externalReference } }
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
                    }
                },
                ExternalReference = externalReference,
                NotificationUrl = $"{_generalsettings.BaseUrl}/webhook/mercadpago",
            };

            Payment payment = await paymentClient.CreateAsync(paymentRequest, requestOptions);

            novoPixPayment.PaymentId = payment.Id.ToString();
            novoPixPayment.Status = MapPaymentStatus(payment.Status);
            novoPixPayment.DateApproved = payment.DateApproved;
            novoPixPayment.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Pagamento PIX {PaymentId} para o usuário {UserId} salvo com sucesso.",
                novoPixPayment.PaymentId, userId);

            // Notifica o front-end sobre o resultado final
            if (payment.Status == "approved" || payment.Status == "pending" || payment.Status == "in_process")
            {
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate("Pagamento processado com sucesso!", "approved", true,
                        payment.Id.ToString()));
            }
            else
            {
                await _notificationService.SendStatusUpdateAsync(
                    userId,
                    new PaymentStatusUpdate(payment.StatusDetail ?? "O pagamento foi recusado.", "failed", true,
                        payment.Id.ToString()));
            }

            return new PaymentResponseDto
            {
                Id = payment.Id.Value,
                Status = payment.Status,
                QrCode = payment.PointOfInteraction?.TransactionData?.QrCode,
                QrCodeBase64 = payment.PointOfInteraction?.TransactionData?.QrCodeBase64,
                Message = "Pagamento PIX criado com sucesso."
            };
        }
        catch (Exception ex)
        {
            var statusFinal = "erro";
            var mensagemErro = "Ocorreu um erro inesperado em nosso sistema.";

            if (ex is MercadoPagoApiException mpex)
            {
                statusFinal = "falhou";
                mensagemErro = mpex.ApiError?.Message ?? "Erro ao comunicar com o provedor.";
                _logger.LogError(mpex, "Erro da API do Mercado Pago: {ApiError}", mpex.ApiError?.Message);
            }
            else
            {
                _logger.LogError(ex, "Erro inesperado ao processar pagamento para {UserId}.", userId);
            }

            // Se o objeto de pagamento foi criado, atualiza seu status para erro.
            if (novoPixPayment != null)
            {
                novoPixPayment.Status = statusFinal;
                novoPixPayment.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(); // Salva o status de erro
            }

            await _notificationService.SendStatusUpdateAsync(
                userId, new PaymentStatusUpdate(mensagemErro, "error", true));

            // Lança uma exceção mais genérica para a camada superior (Controller)
            throw new AppServiceException(mensagemErro, ex);
        }
    }

    public string MapPaymentStatus(string mercadopagoStatus)
    {
        return _statusMap.TryGetValue(mercadopagoStatus, out var status) ? status : "pendente";
    }
}