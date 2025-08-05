using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Plans.DTOs;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using Microsoft.Extensions.Configuration;

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

        public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
            CreateSubscriptionDto subscriptionDto
        )
        {
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                "/preapproval",
                subscriptionDto
            );
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)!;
        }

        // --- MÉTODO AUXILIAR PRIVADO (DRY) ---
        private async Task<string> SendMercadoPagoRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            T? payload
        )
            where T : class
        {
            var request = new HttpRequestMessage(method, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            if (payload != null)
            {
                var jsonContent = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Erro na API do Mercado Pago. Status: {response.StatusCode}. Resposta: {responseBody}"
                );
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
