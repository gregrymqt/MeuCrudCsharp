using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MercadoPagoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PlanResponseDto> CreatePlanAsync(CreatePlanDto planDto)
        {
            // Pega o Access Token do seu appsettings.json ou user-secrets
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException(
                    "Access Token do Mercado Pago não está configurado."
                );
            }

            // Configura o cabeçalho de autorização
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );

            // Serializa o objeto DTO para uma string JSON
            var jsonContent = JsonSerializer.Serialize(planDto);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Faz a chamada POST para a API
            var response = await _httpClient.PostAsync("/preapproval_plan", httpContent);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Se a API retornou um erro, lança uma exceção com os detalhes
                throw new HttpRequestException(
                    $"Erro ao criar plano no Mercado Pago. Status: {response.StatusCode}. Resposta: {responseBody}"
                );
            }

            // Deserializa a resposta JSON para o nosso DTO de resposta
            var planResponse = JsonSerializer.Deserialize<PlanResponseDto>(responseBody);
            return planResponse;
        }

        public async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
            CreateSubscriptionDto subscriptionDto
        )
        {
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException(
                    "Access Token do Mercado Pago não está configurado."
                );
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );

            // O corpo da requisição é o próprio DTO
            var jsonContent = JsonSerializer.Serialize(subscriptionDto);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Faz a chamada POST para o endpoint /preapproval
            var response = await _httpClient.PostAsync("/preapproval", httpContent);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Lança uma exceção com os detalhes do erro retornado pela API do MP
                throw new HttpRequestException(
                    $"Erro ao criar assinatura no Mercado Pago. Status: {response.StatusCode}. Resposta: {responseBody}"
                );
            }

            var subscriptionResponse = JsonSerializer.Deserialize<SubscriptionResponseDto>(
                responseBody
            );
            return subscriptionResponse;
        }
    }
}
