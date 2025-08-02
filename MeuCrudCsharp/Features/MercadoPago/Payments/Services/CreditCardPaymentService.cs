using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Client;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Models; // Garante o acesso a Payment, Subscription, etc.
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // Necessário para Include
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
            return await CreateSinglePaymentInternalAsync(request);
        }

        private async Task<PaymentResponseDto> CreateSinglePaymentInternalAsync(
            PaymentRequestDto paymentData
        )
        {
            var userId = GetCurrentUserId();

            // Substitua 'Payments' pelo nome correto da classe de modelo de pagamento.
            // Supondo que o nome correto do modelo seja 'Payment' (singular), conforme o padrão comum em Entity Framework.
            // Se o nome correto for diferente, substitua por ele.

            var novoPagamento = new Models.Payments
            {
                UserId = userId,
                Status = "iniciando",
                PayerEmail = paymentData?.Payer?.Email,
                Method = paymentData?.PaymentMethodId,
                CustomerCpf = paymentData?.Payer?.Identification?.Number,
                Amount = paymentData.Amount,
                Installments = paymentData.Installments,
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
                NotificationUrl = _configuration["Redirect.Url"]! + "/webhook/mercadopago",
            };

            try
            {
                _context.Payments.Add(novoPagamento); // Usando _context.Payments
                await _context.SaveChangesAsync();

                Payment payment = await _paymentClient.CreateAsync(paymentRequest, requestOptions);

                // Atualizando o registro com os dados da resposta da API
                novoPagamento.ExternalId = payment.Id.ToString()!;
                novoPagamento.Status = MapPaymentStatus(payment.Status);
                novoPagamento.DateApproved = payment.DateApproved;
                novoPagamento.LastFourDigits = int.Parse(payment.Card.LastFourDigits);
                novoPagamento.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return new PaymentResponseDto { Id = payment.Id, Status = payment.Status };
            }
            catch (Exception)
            {
                novoPagamento.Status = "falhou";
                await _context.SaveChangesAsync();
                throw; // Lança a exceção original para ser tratada no Controller
            }
        }

        private async Task<object> CreateSubscriptionInternalAsync(
            PaymentRequestDto subscriptionData
        )
        {
            var userId = GetCurrentUserId();
            var plan = await _context.Plans.FirstOrDefaultAsync(p =>
                p.ExternalPlanId == subscriptionData.PreapprovalPlanId
            );
            if (plan == null)
            {
                throw new InvalidOperationException(
                    "Plano de assinatura não encontrado no banco de dados."
                );
            }

            var createSubscriptionDto = new CreateSubscriptionDto
            {
                PreapprovalPlanId = subscriptionData.PreapprovalPlanId!,
                PayerEmail = subscriptionData?.Payer?.Email,
                CardTokenId = subscriptionData?.Token,
                Reason = plan.Name, // Usa o nome do plano do banco de dados
                BackUrl = _configuration["Redirect.Url"]! + "/Subscription/Success",
            };

            var subscriptionResponse = await _subscriptionService.CreateSubscriptionAsync(
                createSubscriptionDto
            );

            // Usando o model refatorado 'Subscription'
            var novaAssinatura = new Subscription
            {
                UserId = userId,
                PlanId = plan.Id, // Chave estrangeira para o nosso model Plan
                ExternalId = subscriptionResponse.Id,
                Status = MapPaymentStatus(subscriptionResponse.Status),
                PayerEmail = subscriptionData.Payer.Email,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Subscriptions.Add(novaAssinatura);
            await _context.SaveChangesAsync();

            return new { subscriptionResponse.Id, subscriptionResponse.Status };
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
