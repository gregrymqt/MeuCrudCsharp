using System.Security.Claims;
using MercadoPago.Client;
using MercadoPago.Client.Preference;
using MercadoPago.Error;
using MercadoPago.Resource.Preference;
using MercadoPago.Resource.User;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class PreferencePaymentService : IPreferencePayment
    {
        protected readonly TokenMercadoPago _tokenMercadoPago;
        private readonly ApiDbContext _context;
        private readonly IConfiguration _configuration;
        protected readonly PreferenceClient _preferenceClient;

        public PreferencePaymentService(
            TokenMercadoPago tokenMercadoPago,
            ApiDbContext apiDbContext,
            IConfiguration configuration,
            PreferenceClient preferenceClient
        )
        {
            _tokenMercadoPago = tokenMercadoPago;
            _context = apiDbContext;
            _configuration = configuration;
            _preferenceClient = preferenceClient;
        }

        public async Task<Preference> CreatePreferenceAsync(decimal amount, ClaimsPrincipal user)
        {
            try
            {
                var requestOptions = new RequestOptions();
                requestOptions.AccessToken = _tokenMercadoPago._access_Token;
                requestOptions.CustomHeaders.Add("x-idempotency-key", Guid.NewGuid().ToString());

                var profileData = new
                {
                    userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                    name = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                    email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                };

                // Validação para garantir que temos os dados essenciais do pagador.
                if (string.IsNullOrEmpty(profileData.email))
                {
                    throw new InvalidOperationException(
                        "O email do usuário é obrigatório para criar a preferência."
                    );
                }

                var externalReference = Guid.NewGuid().ToString();

                var baseUrl = _configuration["Redirect:Url"];
                var successUrl = $"{baseUrl}/pagamento/success";
                var failureUrl = $"{baseUrl}/pagamento/error";
                var pendingUrl = $"{baseUrl}/pagamento/pending";
                var notificationUrl = baseUrl + "/webhook/mercadopago";

                var preferenceRequest = new PreferenceRequest
                {
                    Items = new List<PreferenceItemRequest>
                    {
                        new PreferenceItemRequest
                        {
                            Id = "CURSO-PSI-01",
                            Title = "Curso Online de Psicologia com Luciana Venancio",
                            Description =
                                "Acesso vitalício ao curso completo, incluindo todos os módulos e materiais de apoio.",
                            CategoryId = "education",
                            Quantity = 1,
                            UnitPrice = amount,
                            CurrencyId = "BRL",
                        },
                    },
                    Payer = new PreferencePayerRequest
                    {
                        Name = profileData.name,
                        Email = profileData.email,
                    },
                    Purpose = "wallet_purchase",
                    ExternalReference = externalReference,
                    NotificationUrl = notificationUrl,
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        Success = successUrl,
                        Failure = failureUrl,
                        Pending = pendingUrl,
                    },
                };

                var preference = await _preferenceClient.CreateAsync(
                    preferenceRequest,
                    requestOptions
                );

                if (preference == null || string.IsNullOrEmpty(preference.Id))
                {
                    throw new InvalidOperationException(
                        "A preferência de pagamento não pôde ser criada. A resposta da API foi nula."
                    );
                }

                // Em caso de sucesso, retorna o objeto de preferência completo.
                // A responsabilidade de retornar um Ok() é do Controller.
                return preference;
            }
            catch (MercadoPagoApiException mpex)
            {
                // Em caso de erro da API do Mercado Pago, logue o erro (opcional) e relance a exceção.
                // Exemplo de log: _logger.LogError(mpex, "Erro na API do Mercado Pago: {Message}", mpex.ApiError.Message);
                throw; // Relança a exceção para a camada superior (Controller) tratar.
            }
            catch (Exception ex)
            {
                // Em caso de erro inesperado, logue e relance.
                // Exemplo de log: _logger.LogError(ex, "Erro inesperado ao criar preferência de pagamento.");
                throw;
            }
        }
    }
}
