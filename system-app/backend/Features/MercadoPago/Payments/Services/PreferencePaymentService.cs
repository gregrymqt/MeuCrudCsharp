using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MercadoPago.Client;
using MercadoPago.Client.Preference;
using MercadoPago.Error;
using MercadoPago.Resource.Preference;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
using MeuCrudCsharp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class PreferencePaymentService : IPreferencePaymentService
{
    private readonly ILogger<PreferencePaymentService> _logger;
    private readonly GeneralSettings _generalSettings;
    private readonly IUserContext _userContext;
    private readonly IUserRepository _userRepository;

    // ADICIONADO: Para salvar o "rastro" do pagamento antes de ir pro MP
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PreferencePaymentService(
        ILogger<PreferencePaymentService> logger,
        IOptions<GeneralSettings> settings,
        IUserContext userContext,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository
    )
    {
        _logger = logger;
        _generalSettings = settings.Value;
        _userContext = userContext;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<string> CreatePreferenceAsync(CreatePreferenceDto model)
    {
        var userId = await _userContext.GetCurrentUserId(); // Pega user completo (com email)
        var user = await _userRepository.GetByIdAsync(userId);
        if (userId == null)
            throw new UnauthorizedAccessException("Usuário não encontrado.");

        if (model.Amount <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.");

        // 1. Gera a Referência que vai ligar o MP ao seu Banco
        var externalReference = Guid.NewGuid().ToString();

        try
        {
            // 2. SALVA NO BANCO (Estado Inicial)
            // Isso é vital para o Webhook funcionar depois
            var initialPayment = new Payments
            {
                UserId = user.Id,
                Status = "pending", // Ou "in_process"
                Amount = model.Amount,
                Method = "preference_checkout", // Para você saber que foi via Link
                ExternalId = externalReference, // O Segredo está aqui!
                PayerEmail = user.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _paymentRepository.AddAsync(initialPayment);
            await _unitOfWork.CommitAsync();

            // 3. Cria a Preferência no MP
            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("x-idempotency-key", Guid.NewGuid().ToString());

            var baseUrl = _generalSettings.BaseUrl;

            var preferenceRequest = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Id = "CURSO-V1", // Pode vir do DTO se quiser
                        Title = model.Title,
                        Description = model.Description,
                        Quantity = 1,
                        UnitPrice = model.Amount,
                        CurrencyId = "BRL",
                    },
                },
                Payer = new PreferencePayerRequest { Name = user.Name, Email = user.Email },
                Purpose = "wallet_purchase",
                ExternalReference = externalReference, // VITAL: Manda o GUID salvo pro MP
                NotificationUrl = $"{baseUrl}/webhook/mercadopago", // O Webhook vai usar esse GUID
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = $"{baseUrl}/pagamento/success",
                    Failure = $"{baseUrl}/pagamento/error",
                    Pending = $"{baseUrl}/pagamento/pending",
                },
                AutoReturn = "approved", // Opcional: Volta pro site automaticamente
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(preferenceRequest, requestOptions);

            _logger.LogInformation(
                "Preferência criada: {PrefId} | Ref: {Ref}",
                preference.Id,
                externalReference
            );

            return preference.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar preferência MP.");
            throw new AppServiceException("Erro ao gerar link de pagamento.", ex);
        }
    }
}
