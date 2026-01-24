using System.Text;
using System.Text.Json;
using MeuCrudCsharp.Features.Exceptions;

namespace MeuCrudCsharp.Features.MercadoPago.Base
{
    /// <summary>
    /// Classe base abstrata para serviços que interagem com a API do Mercado Pago.
    /// Encapsula a lógica comum de envio de requisições HTTP, autenticação e tratamento de erros.
    /// </summary>
    public abstract class MercadoPagoServiceBase
    {
        // Usamos 'protected' para que as classes filhas possam acessar esses campos.
        protected readonly ILogger Logger;
        protected readonly HttpClient HttpClient;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="MercadoPagoServiceBase"/>.
        /// </summary>
        /// <param name="httpClientFactory">A fábrica de clientes HTTP para criar instâncias configuradas.</param>
        /// <param name="logger">O serviço de logging para registrar informações e erros.</param>
        protected MercadoPagoServiceBase(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpClient = httpClientFactory.CreateClient("MercadoPagoClient") 
                ?? throw new ArgumentNullException(nameof(httpClientFactory), "HttpClient não pode ser nulo.");
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Envia uma requisição HTTP genérica e autenticada para a API do Mercado Pago.
        /// </summary>
        /// <typeparam name="T">O tipo do objeto de payload a ser enviado no corpo da requisição.</typeparam>
        /// <param name="method">O método HTTP a ser utilizado (e.g., POST, GET, DELETE).</param>
        /// <param name="endpoint">O caminho do endpoint da API (e.g., "/v1/payments").</param>
        /// <param name="payload">O objeto de dados a ser serializado como JSON e enviado no corpo da requisição. Pode ser nulo.</param>
        /// <returns>Uma string contendo o corpo da resposta da API em caso de sucesso.</returns>
        /// <exception cref="ArgumentNullException">Lançada quando method ou endpoint são nulos.</exception>
        /// <exception cref="InvalidOperationException">Lançada quando BaseAddress não está configurado.</exception>
        /// <exception cref="ExternalApiException">Lançada quando ocorre um erro de comunicação ou a API retorna um status de erro.</exception>
        /// <exception cref="AppServiceException">Lançada para erros inesperados durante o processo.</exception>
        protected async Task<string> SendMercadoPagoRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            T? payload
        )
        {
            // Validações
            ArgumentNullException.ThrowIfNull(method);

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint não pode ser vazio.", nameof(endpoint));

            if (HttpClient.BaseAddress == null)
                throw new InvalidOperationException("HttpClient BaseAddress não está configurado.");

            var requestUri = new Uri(HttpClient.BaseAddress, endpoint);
            var request = new HttpRequestMessage(method, requestUri);

            // Adiciona chave de idempotência para operações de escrita
            if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
            {
                var idempotencyKey = Guid.NewGuid().ToString();
                request.Headers.Add("X-Idempotency-Key", idempotencyKey);
            }

            if (payload != null)
            {
                var jsonPayload = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // Log sem expor dados sensíveis completos
                Logger.LogInformation(
                    "Enviando requisição para MP. Método: {Method}, Endpoint: {Endpoint}",
                    method.Method,
                    endpoint
                );
                
                // Log detalhado apenas em Debug (não em produção)
                Logger.LogDebug("Payload: {Payload}", jsonPayload);
            }

            try
            {
                var response = await HttpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                // Trata erro da API
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError(
                    "Erro na API do Mercado Pago. Status: {StatusCode}. Resposta: {ErrorContent}",
                    response.StatusCode,
                    errorContent
                );
                
                var errorMessage = $"Erro na API do Mercado Pago. Status: {response.StatusCode}. Detalhes: {errorContent}";
                throw new ExternalApiException(
                    errorMessage,
                    new HttpRequestException(errorMessage, null, response.StatusCode)
                );
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(
                    ex,
                    "Erro de comunicação com o provedor de pagamentos. Status: {StatusCode}",
                    ex.StatusCode
                );
                throw new ExternalApiException(
                    "Erro de comunicação com o provedor de pagamentos.",
                    ex
                );
            }
            catch (ExternalApiException)
            {
                // Re-lança ExternalApiException sem envolver
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Erro inesperado ao enviar requisição para o Mercado Pago.");
                throw new AppServiceException("Ocorreu um erro inesperado em nosso sistema.", ex);
            }
        }
    }
}
