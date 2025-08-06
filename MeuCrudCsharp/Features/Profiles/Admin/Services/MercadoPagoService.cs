using MercadoPago.Resource.Customer;
using MeuCrudCsharp.Features.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _accessToken;

        public MercadoPagoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _accessToken =
                _configuration["MercadoPago:AccessToken"]
                ?? throw new InvalidOperationException(
                    "Access Token do Mercado Pago não está configurado."
                );
        }

        public async Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto planDto)
        {
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                "/preapproval_plan",
                planDto
            );
            return JsonSerializer.Deserialize<PlanResponseDto>(responseBody)!;
        }

        public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(string preapprovalPlanId, string cardId, string payerEmail)
        {
            const string endpoint = "/preapproval";

            // 1. Criar o payload com a estrutura correta (aninhando o payer)
            var payload = new SubscriptionWithCardRequestDto
            {
                PreapprovalPlanId = preapprovalPlanId,
                CardId = cardId,
                Payer = new PayerRequestDto // Criando o objeto 'payer' aninhado
                {
                    Email = payerEmail
                }
            };

            // 2. Enviar a requisição para a API do Mercado Pago (esta parte não muda)
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);

            // 3. Desserializar e retornar a resposta (esta parte não muda)
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new InvalidOperationException("Erro ao desserializar a resposta da criação da assinatura.");
        }


        public async Task<RefundResponseDto> RefundPaymentAsync(string paymentId, decimal? amount = null)
        {
            var endpoint = $"/v1/payments/{paymentId}/refunds";
            var payload = new RefundRequestDto { Amount = amount };

            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);

            return JsonSerializer.Deserialize<RefundResponseDto>(responseBody)
                ?? throw new InvalidOperationException("Erro ao desserializar a resposta do reembolso.");
        }

        public async Task<CustomerResponseDto> CreateCustomerAsync(string email, string firstName)
        {
            const string endpoint = "/v1/customers";
            var payload = new CustomerRequestDto
            {
                Email = email,
                FirstName = firstName
            };

            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);

            return JsonSerializer.Deserialize<CustomerResponseDto>(responseBody)
                ?? throw new InvalidOperationException("Erro ao desserializar a resposta da criação do cliente.");
        }

        // =======================================================
        // MÉTODO PARA SALVAR UM CARTÃO NO COFRE DO CLIENTE
        // =======================================================
        public async Task<CardResponseDto> SaveCardToCustomerAsync(string customerId, string cardToken)
        {
            var endpoint = $"/v1/customers/{customerId}/cards";
            var payload = new CardRequestDto { Token = cardToken };

            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);

            return JsonSerializer.Deserialize<CardResponseDto>(responseBody)
                ?? throw new InvalidOperationException("Erro ao desserializar a resposta ao salvar o cartão.");
        }

        // --- MÉTODO AUXILIAR PRIVADO (DRY) ---
        private async Task<string> SendMercadoPagoRequestAsync<T>(HttpMethod method, string endpoint, T? payload) where T : class
        {
            var request = new HttpRequestMessage(method, new Uri($"https://api.mercadopago.com{endpoint}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            if (payload != null)
            {
                var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // Ignora propriedades nulas
                });
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Idealmente, aqui você faria o log do 'responseBody' para depuração
                throw new HttpRequestException($"Erro na API do Mercado Pago. Status: {response.StatusCode}. Resposta: {responseBody}");
            }

            return responseBody;
        }

        public async Task<SubscriptionResponseDto> GetSubscriptionAsync(string subscriptionId)
        {
            var endpoint = $"/preapproval/{subscriptionId}";
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Get,
                endpoint,
                (object?)null
            );
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new InvalidOperationException(
                    "Erro ao desserializar a resposta da assinatura."
                );
        }

        public async Task<PlanSearchResponseDto> SearchPlansAsync()
        {
            // Este método chama o endpoint de busca e desserializa para o novo DTO
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Get,
                "/preapproval_plan/search",
                (object?)null
            );
            return JsonSerializer.Deserialize<PlanSearchResponseDto>(responseBody)!;
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionCardAsync(
            string subscriptionId,
            string cardTokenId
        )
        {
            var endpoint = $"/preapproval/{subscriptionId}";
            var payload = new { card_token_id = cardTokenId };
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new InvalidOperationException(
                    "Erro ao desserializar a resposta da assinatura."
                );
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            string newStatus
        )
        {
            var endpoint = $"/preapproval/{subscriptionId}";
            var payload = new { status = newStatus };
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new InvalidOperationException(
                    "Erro ao desserializar a resposta da assinatura."
                );
        }

        // MÉTODO NOVO IMPLEMENTADO
        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            var endpoint = $"/preapproval/{subscriptionId}";
            var payload = new
            {
                auto_recurring = new { transaction_amount = dto.TransactionAmount },
            };
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new InvalidOperationException(
                    "Erro ao desserializar a resposta da assinatura."
                );
        }

        public async Task<PlanResponseDto> UpdatePlanAsync(string externalPlanId, UpdatePlanDto dto)
        {
            var endpoint = $"/preapproval_plan/{externalPlanId}";

            // O payload agora inclui todos os campos do DTO
            var payload = new
            {
                reason = dto.Reason,
                back_url = dto.BackUrl,
                auto_recurring = new { transaction_amount = dto.TransactionAmount },
            };

            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);

            return JsonSerializer.Deserialize<PlanResponseDto>(responseBody)
                ?? throw new InvalidOperationException(
                    "Erro ao desserializar a resposta da atualização do plano."
                );
        }


    }
}
