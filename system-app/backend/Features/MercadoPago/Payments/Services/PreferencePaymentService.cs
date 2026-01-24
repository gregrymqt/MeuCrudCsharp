using MercadoPago.Client;
using MercadoPago.Client.Preference;
using Microsoft.Extensions.Options;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services;

public class PreferencePaymentService(
    ILogger<PreferencePaymentService> logger,
    IOptions<GeneralSettings> settings,
    IUserContext userContext,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    IUserRepository userRepository)
    : IPreferencePaymentService
{
    private readonly GeneralSettings _generalSettings = settings.Value;


    public async Task<string> CreatePreferenceAsync(CreatePreferenceDto model)
    {
        var userId = await userContext.GetCurrentUserId();
        var user = await userRepository.GetByIdAsync(userId);
        
        if (userId == null)
            throw new UnauthorizedAccessException("Usuário não encontrado.");

        if (model.Amount <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.");

        // 1. Gera a Referência que vai ligar o MP ao seu Banco
        var externalReference = Guid.NewGuid().ToString();

        try
        {
            // 2. Prepara a Entidade (apenas em memória, NÃO persiste ainda)
            var initialPayment = new Models.Payments
            {
                UserId = user.Id,
                Status = "pending",
                Amount = model.Amount,
                Method = "preference_checkout",
                ExternalId = externalReference,
                PayerEmail = user.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Marca para adição (NÃO persiste ainda)
            await paymentRepository.AddAsync(initialPayment);

            // 3. Cria a Preferência no MercadoPago
            var requestOptions = new RequestOptions();
            requestOptions.CustomHeaders.Add("x-idempotency-key", Guid.NewGuid().ToString());

            var baseUrl = _generalSettings.BaseUrl;

            var preferenceRequest = new PreferenceRequest
            {
                Items =
                [
                    new PreferenceItemRequest
                    {
                        Id = "CURSO-V1",
                        Title = model.Title,
                        Description = model.Description,
                        Quantity = 1,
                        UnitPrice = model.Amount,
                        CurrencyId = "BRL",
                    },
                ],
                Payer = new PreferencePayerRequest { Name = user.Name, Email = user.Email },
                Purpose = "wallet_purchase",
                ExternalReference = externalReference,
                NotificationUrl = $"{baseUrl}/webhook/mercadopago",
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = $"{baseUrl}/pagamento/success",
                    Failure = $"{baseUrl}/pagamento/error",
                    Pending = $"{baseUrl}/pagamento/pending",
                },
                AutoReturn = "approved",
            };

            var client = new PreferenceClient();
            var preference = await client.CreateAsync(preferenceRequest, requestOptions);

            // 4. Atualiza o Payment com o ID da Preferência (ainda em memória)
            initialPayment.PaymentId = preference.Id;
            initialPayment.UpdatedAt = DateTime.UtcNow;

            // 5. ✅ COMMIT ÚNICO - ATOMICIDADE GARANTIDA
            // Persiste o payment com TODOS os dados preenchidos (incluindo Preference ID do MP)
            await unitOfWork.CommitAsync();

            logger.LogInformation(
                "Preferência criada com sucesso: {PrefId} | ExternalRef: {Ref} | UserId: {UserId}",
                preference.Id,
                externalReference,
                userId
            );

            return preference.Id;
        }
        catch (Exception ex)
        {
            // ✅ ROLLBACK AUTOMÁTICO
            // Como não fizemos commit ainda, o UnitOfWork descarta todas as mudanças automaticamente
            logger.LogError(
                ex, 
                "Erro ao criar preferência de pagamento para o usuário {UserId}. Rollback automático.",
                userId
            );
            
            throw new AppServiceException("Erro ao gerar link de pagamento.", ex);
        }
    }
}
