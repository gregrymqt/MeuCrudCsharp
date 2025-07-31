using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SubscriptionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri("https://api.mercadopago.com");
        }

        private void SetAuthorizationHeader()
        {
            var accessToken = _configuration["MercadoPago:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Access Token do Mercado Pago não configurado.");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public async Task<string> SearchSubscriptionAsync(string searchParameter)
        {
            SetAuthorizationHeader();
            // A API do MP permite buscar por vários parâmetros, aqui usamos um genérico
            var response = await _httpClient.GetAsync($"/preapproval/search?q={Uri.EscapeDataString(searchParameter)}");
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Erro ao buscar assinatura: {content}");
            return content; // Retorna o JSON bruto da resposta
        }

        public async Task<string> UpdateSubscriptionValueAsync(string id, UpdateSubscriptionValueDto dto)
        {
            SetAuthorizationHeader();
            var recurringData = new { auto_recurring = dto };
            var jsonContent = JsonSerializer.Serialize(recurringData);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/preapproval/{id}", httpContent);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Erro ao alterar valor da assinatura: {content}");
            return content;
        }

        public async Task<string> UpdateSubscriptionStatusAsync(string id, UpdateSubscriptionStatusDto dto)
        {
            SetAuthorizationHeader();
            var jsonContent = JsonSerializer.Serialize(dto);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/preapproval/{id}", httpContent);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Erro ao alterar status da assinatura: {content}");
            return content;
        }
    }
}
