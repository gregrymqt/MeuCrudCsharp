using MercadoPago.Client;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using MeuCrudCsharp.Data; // Supondo que seu DbContext e Pagamento estão aqui
using MeuCrudCsharp.Models; // Supondo que sua entidade Pagamento está aqui
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Services
{
    public class CreditCardPaymentService // Nome da classe alterado para seguir convenções
    {
        // Campos privados com convenção de nomenclatura _camelCase
        private readonly TokenMercadoPago _tokenMercadoPago;
        private readonly PaymentClient _paymentClient;
        private readonly ApiDbContext _context;

        // Dicionário para mapear os status, agora com a sintaxe correta de C#
        private readonly Dictionary<string, string> _statusMap = new()
        {
            { "approved", "aprovado" },
            { "pending", "pendente" },
            { "in_process", "pendente" },
            { "rejected", "recusado" },
            { "refunded", "reembolsado" },
            { "cancelled", "cancelado" }
        };

        // Construtor com injeção de dependência
        public CreditCardPaymentService(
            TokenMercadoPago tokenMercadoPago,
            PaymentClient paymentClient,
            ApiDbContext context)
        {
            _tokenMercadoPago = tokenMercadoPago;
            _paymentClient = paymentClient;
            _context = context;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(PaymentRequestDto paymentData, long userId, decimal transactionAmount)
        {
            try
            {
                // 1. Criação do objeto de requisição fortemente tipado
                var paymentRequest = new PaymentCreateRequest
                {
                    TransactionAmount = transactionAmount,
                    Token = paymentData.Token,
                    Description = "Curso Online", // Exemplo de descrição
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
                    },
                    NotificationUrl = "https://seu-site.com/api/notifications" // Preencha com sua URL real
                };

                // 2. Configuração do Access Token por requisição (Thread-Safe)
                var requestOptions = new RequestOptions
                {
                    AccessToken = _tokenMercadoPago._access_Token
                };

                // 3. Chamada assíncrona para a API do Mercado Pago
                Payment payment = await _paymentClient.CreateAsync(paymentRequest, requestOptions);

                // 4. Criação da entidade para salvar no banco de dados
                var novoPagamento = new Pagamento // Supondo que você tenha uma classe "Pagamento"
                {
                    PaymentId = payment.Id.ToString(),
                    UserId = userId,
                    Status = MapPaymentStatus(payment.Status),
                    method = "cartao_credito",
                    Installments = payment.Installments ?? 0,
                    amount = transactionAmount,
                };

                _context.Pagamentos.Add(novoPagamento);
                await _context.SaveChangesAsync();

                // 5. Retorno dos dados relevantes usando o DTO de resposta
                return new PaymentResponseDto
                {
                    Status = payment.Status,
                    Id = payment.Id,
                    PaymentTypeId = payment.PaymentTypeId
                };
            }
            catch (MercadoPagoApiException ex)
            {
                // Captura erros específicos da API e lança uma exceção clara
                throw new Exception($"Erro na API do Mercado Pago: {ex.ApiError.Message}", ex);
            }
            catch (Exception ex)
            {
                // Captura outros erros e lança uma exceção genérica
                throw new Exception("Erro inesperado ao processar o pagamento.", ex);
            }
        }

        // Método auxiliar para mapear o status do pagamento
        private string MapPaymentStatus(string mercadopagoStatus)
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