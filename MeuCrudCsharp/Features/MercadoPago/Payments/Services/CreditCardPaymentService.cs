using System;
using System.Collections.Generic; // Adicionado para Dictionary
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Client;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using MercadoPago.Resource.User;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Models; // Garanta que este using exista
using Microsoft.AspNetCore.Http; // Adicionado para IHttpContextAccessor
using Microsoft.Extensions.Configuration;

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

        // DICIONÁRIO DE STATUS CORRIGIDO
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
            TokenMercadoPago tokenMercadoPago,
            PaymentClient paymentClient,
            ApiDbContext context,
            IConfiguration configuration,
            IMercadoPagoService subscriptionService,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _tokenMercadoPago = tokenMercadoPago;
            _paymentClient = paymentClient;
            _context = context;
            _configuration = configuration;
            _subscriptionService = subscriptionService;
            _httpContextAccessor = httpContextAccessor;
        }

        // Método público que implementa a interface
        public async Task<object> CreatePaymentOrSubscriptionAsync(PaymentRequestDto request)
        {
            if (string.Equals(request.Plano, "anual", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateSubscriptionInternalAsync(request);
            }
            else
            {
                return await CreateSinglePaymentInternalAsync(request);
            }
        }

        // Método auxiliar PRIVADO
        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(
            PaymentRequestDto paymentData
        )
        {
            var userId = GetCurrentUserId();
            var novoPagamento = new Payment_User
            {
                UserId = userId,
                Status = "iniciando",
                Method = "cartao_credito",
                Amount = paymentData.Amount,
            };

            var requestOptions = new RequestOptions
            {
                AccessToken = _tokenMercadoPago._access_Token!,
            };
            requestOptions.CustomHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = paymentData.Amount,
                Token = paymentData.Token,
                Description = _configuration["Defaultdescription"],
                Installments = paymentData.Installments,
                PaymentMethodId = paymentData.PaymentMethodId,
                Payer = new PaymentPayerRequest { Email = paymentData.Payer.Email },
                ExternalReference = novoPagamento.Id.ToString(),
                NotificationUrl = _configuration["Redirect.Url"] + "/webhook/mercadopago",
            };

            try
            {
                _context.Payment_User.Add(novoPagamento);
                await _context.SaveChangesAsync();
                Payment payment = await _paymentClient.CreateAsync(paymentRequest, requestOptions);
                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status = MapPaymentStatus(payment.Status);
                await _context.SaveChangesAsync();
                return new PaymentResponseDto { Id = payment.Id, Status = payment.Status };
            }
            catch (Exception ex)
            {
                novoPagamento.Status = "falhou";
                await _context.SaveChangesAsync();
                throw new Exception("Erro ao processar pagamento único.", ex);
            }
        }

        // Método auxiliar PRIVADO
        private async Task<object> CreateSubscriptionInternalAsync(
            PaymentRequestDto subscriptionData
        )
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(subscriptionData.PreapprovalPlanId))
            {
                throw new ArgumentException("O ID do plano de assinatura é obrigatório.");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.ToString());

            var createSubscriptionDto = new CreateSubscriptionDto
            {
                PreapprovalPlanId = subscriptionData.PreapprovalPlanId,
                PayerEmail = subscriptionData.Payer.Email,
                CardTokenId = subscriptionData.Token,
                Reason = $"Assinatura plano anual para {subscriptionData.Payer.Email}",
                BackUrl = _configuration["Redirect.Url"]! + "/Subscription/Success",
            };

            try
            {
                var subscriptionResponse = await _subscriptionService.CreateSubscriptionAsync(
                    createSubscriptionDto
                );

                // CRIAÇÃO DO MODELO DE ASSINATURA CORRIGIDO
                var novaAssinatura = new Subscription
                {
                    UserId = userId,
                    SubscriptionId = subscriptionResponse.Id,
                    PlanId = subscriptionData.PreapprovalPlanId,
                    Status = subscriptionResponse.Status,
                    PayerEmail = user?.Email, // CORREÇÃO AQUI
                };

                // ACESSO AO DBCONTEXT CORRIGIDO
                _context.Subscriptions.Add(novaAssinatura);
                await _context.SaveChangesAsync();

                return new
                {
                    SubscriptionId = subscriptionResponse.Id,
                    Status = subscriptionResponse.Status,
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao criar a assinatura no Mercado Pago.", ex);
            }
        }

        public string MapPaymentStatus(string mercadopagoStatus)
        {
            return _statusMap.TryGetValue(mercadopagoStatus, out var status) ? status : "pendente";
        }

        private Guid GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );
            if (!Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                throw new InvalidOperationException("ID do usuário inválido ou não encontrado.");
            }
            return userIdGuid;
        }
    }
}
