using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data; // Supondo que seu DbContext e Pagamento estão aqui
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Models; // Supondo que sua entidade Pagamento está aqui
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class CreditCardPaymentService : ICreditCardPayment // Nome da classe alterado para seguir convenções
    {
        // Campos privados com convenção de nomenclatura _camelCase
        private readonly TokenMercadoPago _tokenMercadoPago;
        private readonly PaymentClient _paymentClient;
        private readonly ApiDbContext _context;
        private readonly IConfiguration _configuration;

        // Dicionário para mapear os status, agora com a sintaxe correta de C#
        private readonly Dictionary<string, string> _statusMap = new()
        {
            { "approved", "aprovado" },
            { "pending", "pendente" },
            { "in_process", "pendente" },
            { "rejected", "recusado" },
            { "refunded", "reembolsado" },
            { "cancelled", "cancelado" },
        };

        // Construtor com injeção de dependência
        public CreditCardPaymentService(
            TokenMercadoPago tokenMercadoPago,
            PaymentClient paymentClient,
            ApiDbContext context,
            IConfiguration configuration
        )
        {
            _tokenMercadoPago = tokenMercadoPago;
            _paymentClient = paymentClient;
            _context = context;
            _configuration = configuration;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(
            PaymentRequestDto paymentData,
            decimal transactionAmount
        )
        {
            var user = new ClaimsPrincipal();
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out Guid userIdGuid))
            {
                throw new InvalidOperationException("O ID do usuário não esta correto");
            }

            // PASSO 1: Crie a entidade local PRIMEIRO, mas ainda não salve.
            // O construtor já gera o nosso ID local único (novoPagamento.Id).
            var novoPagamento = new Payment_User
            {
                UserId = userIdGuid,
                Status = "iniciando", // Um status inicial para indicar que a transação começou
                Method = "cartao_credito",
                Amount = transactionAmount,
                // O PaymentId do Mercado Pago ainda é nulo, o que está correto.
            };

            // PASSO 2: Configure a requisição para o Mercado Pago
            var requestOptions = new RequestOptions();
            requestOptions.AccessToken = _tokenMercadoPago._access_Token;
            requestOptions.CustomHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            var paymentRequest = new PaymentCreateRequest
            {
                TransactionAmount = transactionAmount,
                Token = paymentData.Token,
                Description = _configuration["Defaultdescription"],
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
                NotificationUrl = _configuration["Redirect.Url"] + "/webhook/mercadopago",

                // AQUI ESTÁ A MÁGICA:
                // Enviamos o ID do NOSSO banco de dados para o Mercado Pago.
                ExternalReference = novoPagamento.Id.ToString(),
            };

            try
            {
                // PASSO 3: Salve a entidade no banco ANTES da chamada da API.
                // Agora temos um registro persistido da tentativa de pagamento.
                _context.Payment_User.Add(novoPagamento);
                await _context.SaveChangesAsync();

                // PASSO 4: Chame a API do Mercado Pago
                Payment payment = await _paymentClient.CreateAsync(paymentRequest, requestOptions);

                novoPagamento.PaymentId = payment.Id.ToString();
                novoPagamento.Status = MapPaymentStatus(payment.Status);
                novoPagamento.DateApproved = payment.DateApproved;
                if (
                    payment.Card != null
                    && int.TryParse(payment.Card.LastFourDigits, out int lastFourDigits)
                )
                {
                    novoPagamento.LastFourDigits = lastFourDigits;
                }
                novoPagamento.Installments = payment.Installments ?? 1;
                novoPagamento.Method = payment.PaymentMethodId;
                novoPagamento.CustomerCpf = payment.Payer.Identification.Number;
                await _context.SaveChangesAsync();

                // PASSO 6: Retorne a resposta para o frontend
                return new PaymentResponseDto
                {
                    Status = payment.Status,
                    Id = payment.Id,
                    PaymentTypeId = payment.PaymentTypeId,
                };
            }
            catch (MercadoPagoApiException ex)
            {
                // Se a API falhar, atualizamos nosso registro para "falhou".
                novoPagamento.Status = "falhou";
                await _context.SaveChangesAsync();
                throw new Exception($"Erro na API do Mercado Pago: {ex.ApiError.Message}", ex);
            }
            catch (Exception ex)
            {
                novoPagamento.Status = "erro_interno";
                await _context.SaveChangesAsync();
                throw new Exception("Erro inesperado ao processar o pagamento.", ex);
            }
        }

        // Método auxiliar para mapear o status do pagamento
        public string MapPaymentStatus(string mercadopagoStatus)
        {
            // TryGetValue é a forma segura de acessar um dicionário
            if (_statusMap.TryGetValue(mercadopagoStatus, out var status))
            {
                return status;
            }
            return "pendente"; // Valor padrão caso o status não seja encontrado
        }
    }
}
