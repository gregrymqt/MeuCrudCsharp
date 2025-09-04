using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services;

public class PixPaymentService : IPixPaymentService
{
    private readonly ILogger<PixPaymentService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IPaymentNotificationService _notificationService;
    private readonly ApiDbContext _dbContext;
    private readonly PaymentSettings _settings;

    public PixPaymentService(ILogger<PixPaymentService> logger,
        ICacheService cacheService,
        IPaymentNotificationService notificationService,
        ApiDbContext dbContext,
        PaymentSettings settings)
    {
        _logger = logger;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _dbContext = dbContext;
        _settings = settings;
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

    public async Task<PaymentResponseDto> CreatePixPaymentAsync(string userId, CreatePixPaymentRequest request)
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
                ExternalId = Guid.NewGuid().ToString(),
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

            var paymentClient = new PaymentClient();
            var requestOptions = new RequestOptions
            {
                CustomHeaders = { { "X-Idempotency-Key", novoPixPayment.ExternalId } }
            };

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
                ExternalReference = novoPixPayment.ExternalId,
                NotificationUrl = _settings.NotificationUrl,
            };

            Payment payment = await paymentClient.CreateAsync(paymentRequest, requestOptions);

            novoPixPayment.PaymentId = payment.Id.ToString();
            novoPixPayment.Status = MapPaymentStatus(payment.Status);
            novoPixPayment.DateApproved = payment.DateApproved;
            novoPixPayment.UpdatedAt = DateTime.UtcNow;

            var cacheKey = $"payment:{payment.Id}";
            var cacheDuration = TimeSpan.FromMinutes(30);
            await _cacheService.SetAsync(cacheKey, payment, cacheDuration);
            _logger.LogInformation("Objeto de pagamento {PaymentId} armazenado em cache.", payment.Id);

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