using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MercadoPago.Client;
using MeuCrudCsharp.Features.Exceptions;

namespace MeuCrudCsharp.Features.MercadoPago.Base
{
    public abstract class MercadoPagoServiceBase
    {
        // Usamos 'protected' para que as classes filhas possam acessar esses campos.
        protected readonly ILogger _logger;
        protected readonly HttpClient _httpClient;

        // O construtor da classe base recebe as dependências comuns.
        protected MercadoPagoServiceBase(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Método genérico e protegido para enviar requisições para a API do Mercado Pago.
        /// </summary>
        protected async Task<string> SendMercadoPagoRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            T? payload
        )
            where T : class
        {
            var requestOptions = new RequestOptions();
            var request = new HttpRequestMessage(
                method,
                new Uri($"https://api.mercadopago.com{endpoint}")
            );
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                requestOptions.AccessToken
            );
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            if (payload != null)
            {
                var jsonContent = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Erro da API do Mercado Pago. Status: {StatusCode}. Endpoint: {Endpoint}. Resposta: {ResponseBody}",
                        response.StatusCode,
                        endpoint,
                        responseBody
                    );
                    throw new HttpRequestException(
                        $"Erro na API do Mercado Pago: {responseBody}",
                        null,
                        response.StatusCode
                    );
                }

                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                throw new ExternalApiException(
                    "Erro de comunicação com o provedor de pagamentos.",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar requisição para o Mercado Pago.");
                throw new AppServiceException("Ocorreu um erro inesperado em nosso sistema.", ex);
            }
        }
    }
}
