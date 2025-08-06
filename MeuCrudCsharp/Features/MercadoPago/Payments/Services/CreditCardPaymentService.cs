using MercadoPago.Client.Payment;
using MercadoPago.Client.Common;
using MercadoPago.Resource.Payment;
using MercadoPago.Client;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MercadoPago.Error;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class CreditCardPaymentService : ICreditCardPayments
    {
        private readonly TokenMercadoPago _tokenMercadoPago;
        private readonly PaymentClient _paymentClient;
        private readonly ApiDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMercadoPagoService _subscriptionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreditCardPaymentService> _logger; // MUDANÇA 1

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
            TokenMercadoPago tokenMercadoPago, PaymentClient paymentClient, ApiDbContext context,
            IConfiguration configuration, IMercadoPagoService subscriptionService,
            IHttpContextAccessor httpContextAccessor, ILogger<CreditCardPaymentService> logger) // MUDANÇA 1
        {
            _tokenMercadoPago = tokenMercadoPago;
            _paymentClient = paymentClient;
            _context = context;
            _configuration = configuration;
            _subscriptionService = subscriptionService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger; // MUDANÇA 1
        }

        public async Task<object> CreatePaymentOrSubscriptionAsync(PaymentRequestDto request)
        {
            // MUDANÇA 2: Validação "Fail-Fast"
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Os dados do pagamento não podem ser nulos.");

            if (string.Equals(request.Plano, "anual", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateSubscriptionInternalAsync(request);
            }
            return await CreateSinglePaymentInternalAsync(request);
        }

        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(PaymentRequestDto paymentData)
        {
            var userId = GetCurrentUserId();

            // É uma boa prática validar os dados recebidos antes de prosseguir.
            if (paymentData.Payer?.Email is null || paymentData.Payer.Identification?.Number is null)
            {
                throw new ArgumentException("Dados do pagador (email, CPF) são obrigatórios.");
            }

            var novoPagamento = new Models.Payments
            {
                UserId = userId,
                Status = "iniciando",
                PayerEmail = paymentData.Payer.Email,
                Method = paymentData.PaymentMethodId,
                CustomerCpf = paymentData.Payer.Identification.Number,
                Amount = paymentData.Amount,
                Installments = paymentData.Installments,
                ExternalId = Guid.NewGuid().ToString(), // Usando um ID externo único para idempotência
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Salva o registro inicial no banco
            _context.Payments.Add(novoPagamento);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Registro de pagamento inicial criado no banco com ID: {PaymentId}", novoPagamento.Id);

            try
            {
                var requestOptions = new RequestOptions { AccessToken = _tokenMercadoPago._access_Token! };
                // A chave de idempotência garante que, se a mesma requisição for feita múltiplas vezes
                // (ex: por um problema de rede), o pagamento não será processado duas vezes.
                requestOptions.CustomHeaders.Add("X-Idempotency-Key", novoPagamento.ExternalId);

                // =======================================================
                //  INÍCIO DA CORREÇÃO PRINCIPAL
                // =======================================================
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
                            Number = paymentData.Payer.Identification.Number
                        }
                    }
                };
                // =======================================================
                //  FIM DA CORREÇÃO PRINCIPAL
                // =======================================================

                Payment payment = await _paymentClient.CreateAsync(paymentRequest, requestOptions);

                // Atualiza o registro no banco com os dados retornados pela API
                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status = MapPaymentStatus(payment.Status); // Supondo que você tenha este método de mapeamento
                novoPagamento.DateApproved = payment.DateApproved;
                novoPagamento.UpdatedAt = DateTime.UtcNow;

                // **MELHORIA DE SEGURANÇA**: Evita erro se 'Card' ou 'LastFourDigits' for nulo.
                if (int.TryParse(payment.Card?.LastFourDigits, out int lastFourDigits))
                {
                    novoPagamento.LastFourDigits = lastFourDigits;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Pagamento {PaymentId} processado e atualizado no banco. Status: {Status}", novoPagamento.PaymentId, novoPagamento.Status);

                return new PaymentResponseDto { Id = payment.Id, Status = payment.Status, Message = "Pagamento processado com sucesso." };
            }
            catch (MercadoPagoApiException mpex)
            {
                novoPagamento.Status = "falhou";
                await _context.SaveChangesAsync();
                _logger.LogError(mpex, "Erro da API do Mercado Pago ao processar pagamento para o usuário {UserId}. Erro: {ApiError}", userId, mpex.ApiError.Message);
                // Retorna uma mensagem de erro mais amigável para o frontend
                throw new ExternalApiException(mpex.ApiError?.Message ?? "Ocorreu um erro ao processar seu pagamento com nosso provedor.", mpex);
            }
            catch (Exception ex)
            {
                novoPagamento.Status = "erro_interno";
                await _context.SaveChangesAsync();
                _logger.LogError(ex, "Erro inesperado ao processar pagamento para o usuário {UserId}.", userId);
                throw new AppServiceException("Ocorreu um erro inesperado em nosso sistema ao processar o pagamento.", ex);
            }
        }

        private async Task<object> CreateSubscriptionInternalAsync(PaymentRequestDto subscriptionData)
        {
            var userId = GetCurrentUserId();

            // MUDANÇA 3: Bloco try-catch robusto
            try
            {
                var plan = await _context.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.ExternalPlanId == subscriptionData.PreapprovalPlanId);
                if (plan == null)
                {
                    throw new ResourceNotFoundException($"O plano com ID externo '{subscriptionData.PreapprovalPlanId}' não foi encontrado em nosso sistema.");
                }

                var createSubscriptionDto = new CreateSubscriptionDto
                {
                    PreapprovalPlanId = subscriptionData.PreapprovalPlanId!,
                    PayerEmail = subscriptionData.Payer.Email,
                    CardTokenId = subscriptionData.Token,
                    Reason = plan.Name,
                    BackUrl = _configuration["Redirect.Url"]! + "/Subscription/Success",
                };

                var subscriptionResponse = await _subscriptionService.CreateSubscriptionAsync(createSubscriptionDto);

                var novaAssinatura = new Subscription
                {
                    UserId = userId,
                    PlanId = plan.Id,
                    ExternalId = subscriptionResponse.Id,
                    Status = MapPaymentStatus(subscriptionResponse.Status),
                    PayerEmail = subscriptionData.Payer.Email,
                    UpdatedAt = DateTime.UtcNow,
                    PaymentId = subscriptionResponse.Id,
                };

                _context.Subscriptions.Add(novaAssinatura);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Assinatura {SubscriptionId} criada com sucesso para o usuário {UserId}.", novaAssinatura.ExternalId, userId);

                return new { subscriptionResponse.Id, subscriptionResponse.Status };
            }
            catch (ResourceNotFoundException) // Deixa a exceção específica passar para o controller
            {
                throw;
            }
            catch (Exception ex) // Captura qualquer outro erro (da API do MP ou do banco)
            {
                _logger.LogError(ex, "Erro inesperado ao criar assinatura para o usuário {UserId}.", userId);
                throw new AppServiceException("Ocorreu um erro inesperado em nosso sistema ao criar a assinatura.", ex);
            }
        }

        public string MapPaymentStatus(string mercadopagoStatus)
        {
            return _statusMap.TryGetValue(mercadopagoStatus, out var status) ? status : "pendente";
        }

        private Guid GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                // MUDANÇA 4: Exceção mais específica
                throw new AppServiceException("A identificação do usuário não pôde ser encontrada na sessão atual.");
            }
            return userIdGuid;
        }
    }
}