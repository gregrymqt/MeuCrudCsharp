using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MercadoPago.Client;
using MercadoPago.Config;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeuCrudCsharp.Features.MercadoPago.Base
{
    /// <summary>
    /// Classe base abstrata para serviços que interagem com a API do Mercado Pago.
    /// Encapsula a lógica comum de envio de requisições HTTP, autenticação e tratamento de erros.
    /// </summary>
    public abstract class MercadoPagoServiceBase
    {
        // Usamos 'protected' para que as classes filhas possam acessar esses campos.
        protected readonly ILogger _logger;
        protected readonly HttpClient _httpClient;

        // O construtor da classe base recebe as dependências comuns.
        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="MercadoPagoServiceBase"/>.
        /// </summary>
        /// <param name="httpClient">O cliente HTTP para realizar as requisições.</param>
        /// <param name="logger">O serviço de logging para registrar informações e erros.</param>
        protected MercadoPagoServiceBase(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            // A fábrica cria um cliente com a configuração "MercadoPagoClient" que definimos no Program.cs
            _httpClient = httpClientFactory.CreateClient("MercadoPagoClient"); 
            _logger = logger;
        }

        /// &lt;summary&gt;
        /// Método genérico e protegido para enviar requisições para a API do Mercado Pago.
        /// &lt;/summary&gt;
        /// <summary>
        /// Envia uma requisição HTTP genérica e autenticada para a API do Mercado Pago.
        /// </summary>
        /// <typeparam name="T">O tipo do objeto de payload a ser enviado no corpo da requisição.</typeparam>
        /// <param name="method">O método HTTP a ser utilizado (e.g., POST, GET, DELETE).</param>
        /// <param name="endpoint">O caminho do endpoint da API (e.g., "/v1/payments").</param>
        /// <param name="payload">O objeto de dados a ser serializado como JSON e enviado no corpo da requisição. Pode ser nulo.</param>
        /// <returns>Uma string contendo o corpo da resposta da API em caso de sucesso.</returns>
        /// <exception cref="ExternalApiException">Lançada quando ocorre um erro de comunicação ou a API retorna um status de erro.</exception>
        /// <exception cref="AppServiceException">Lançada para erros inesperados durante o processo.</exception>
        public async Task<string> SendMercadoPagoRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            T? payload
        )
        {
            var requestUri = new Uri(_httpClient.BaseAddress, endpoint);
            
            var request = new HttpRequestMessage(
                method,
                requestUri
            );

            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());

            if (payload != null)
            {
                // Usando as opções de serialização padrão do ASP.NET Core para consistência
                var jsonContent = JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    }
                );
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }
            try
            {
                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                return responseBody;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de comunicação com o provedor de pagamentos. Status: {StatusCode}",
                    ex.StatusCode);
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