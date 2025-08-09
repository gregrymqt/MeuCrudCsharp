using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Client;
using MercadoPago.Client.Preference;
using MercadoPago.Error;
using MercadoPago.Resource.Preference;
using MeuCrudCsharp.Features.Exceptions; // Nossas exceções
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Injetando o Logger

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services
{
    public class PreferencePaymentService : IPreferencePayment
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PreferencePaymentService> _logger;

        public PreferencePaymentService(
            IConfiguration configuration,
            ILogger<PreferencePaymentService> logger
        ) // Adicionando ILogger
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Preference> CreatePreferenceAsync(decimal amount, ClaimsPrincipal user)
        {
            // Validação "Fail-Fast"
            if (user?.Identity?.IsAuthenticated != true)
                throw new ArgumentException("Usuário não autenticado.", nameof(user));

            var userEmail = user.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
                throw new ArgumentException("O e-mail do usuário é obrigatório.", nameof(user));

            var userName = user.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException("O userName do usuário é obrigatório.", nameof(user));

            if (amount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "O valor deve ser maior que zero."
                );

            try
            {
                var preferenceClient = new PreferenceClient();
                var requestOptions = new RequestOptions();
                requestOptions.CustomHeaders.Add("x-idempotency-key", Guid.NewGuid().ToString());

                var baseUrl =
                    _configuration["Redirect:Url"]
                    ?? throw new InvalidOperationException(
                        "A URL de redirecionamento não está configurada."
                    );

                var externalReference = Guid.NewGuid().ToString();

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
                    Payer = new PreferencePayerRequest { Name = userName, Email = userEmail },
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

                _logger.LogInformation(
                    "Criando preferência de pagamento para o usuário {UserEmail} no valor de {Amount}",
                    userEmail,
                    amount
                );

                Preference preference = await preferenceClient.CreateAsync(
                    preferenceRequest,
                    requestOptions
                );

                if (preference == null || string.IsNullOrEmpty(preference.Id))
                {
                    throw new AppServiceException(
                        "A resposta da API do gateway de pagamento foi inválida ao criar a preferência."
                    );
                }

                _logger.LogInformation(
                    "Preferência {PreferenceId} criada com sucesso.",
                    preference.Id
                );
                return preference;
            }
            catch (MercadoPagoApiException mpex)
            {
                _logger.LogError(
                    mpex,
                    "Erro na API do Mercado Pago ao criar preferência para {UserEmail}. Erro: {ApiError}",
                    userEmail,
                    mpex.ApiError.Message
                );
                throw new ExternalApiException(
                    "Ocorreu um erro ao comunicar com o gateway de pagamento.",
                    mpex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao criar preferência para {UserEmail}.",
                    userEmail
                );
                throw new AppServiceException("Ocorreu um erro inesperado em nosso sistema.", ex);
            }
        }
    }
}
