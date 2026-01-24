using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Shared.Work;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Services;

/// <summary>
/// Service responsável por gerenciar a carteira de cartões dos clientes.
/// Coordena operações entre o banco local (User) e a API do Mercado Pago (Customer/Cards).
/// </summary>
public class ClientService(
    IClientMercadoPagoService mpService,
    ICacheService cacheService,
    IUserRepository userRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork unitOfWork,
    ILogger<ClientService> logger)
    : IClientService
{

    /// <summary>
    /// Obtém a carteira de cartões de um usuário.
    /// Combina dados do Mercado Pago com informações de assinatura ativa.
    /// Utiliza cache de 15 minutos.
    /// </summary>
    public async Task<List<WalletCardDto>> GetUserWalletAsync(string userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ResourceNotFoundException("Usuário não encontrado.");

        if (string.IsNullOrEmpty(user.CustomerId))
            return [];

        // 1. Busca cartões do Mercado Pago (com cache)
        var mpCards = await ListCardsFromCustomerAsync(user.CustomerId);

        // 2. Busca assinatura ativa para marcar o cartão principal
        var activeSubscription = await subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);

        // 3. Mapeia Record -> Class
        return mpCards
            .Select(card => new WalletCardDto
            {
                Id = card.Id ?? "",
                LastFourDigits = card.LastFourDigits ?? "****",
                ExpirationMonth = card.ExpirationMonth ?? 0,
                ExpirationYear = card.ExpirationYear ?? 0,
                // Pega o ID de dentro do objeto PaymentMethod ou define unknown
                PaymentMethodId = card.PaymentMethod?.Id ?? "unknown",
                IsSubscriptionActiveCard =
                    activeSubscription != null && activeSubscription.CardTokenId == card.Id,
            })
            .ToList();
    }

    /// <summary>
    /// Adiciona um cartão à carteira do usuário.
    /// Se o usuário não tiver CustomerId, cria um Customer no Mercado Pago primeiro.
    /// Usa UnitOfWork para garantir que a atualização do User seja persistida.
    /// </summary>
    public async Task<WalletCardDto> AddCardToWalletAsync(string userId, string cardToken)
    {
        // Validação de entrada
        if (string.IsNullOrWhiteSpace(cardToken))
            throw new ArgumentException("Token do cartão não pode ser vazio.", nameof(cardToken));

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ResourceNotFoundException("Usuário não encontrado.");

        try
        {
            CardInCustomerResponseDto resultCard;
            if (string.IsNullOrEmpty(user.CustomerId))
            {
                logger.LogInformation("Usuário {UserId} não tem CustomerId. Criando Customer no MP.", userId);

                // 1. Cria Customer no Mercado Pago
                var newCustomer = await mpService.CreateCustomerAsync(user.Email!, user.Name!);
                
                // 2. Atualiza User no banco local
                user.CustomerId = newCustomer.Id;
                userRepository.Update(user); // ✅ Marca para Update

                // 3. Adiciona o cartão ao Customer criado
                resultCard = await AddCardToCustomerAsync(newCustomer.Id!, cardToken);

                // ✅ 4. COMMIT - Salva a atualização do User
                await unitOfWork.CommitAsync();

                logger.LogInformation("Customer {CustomerId} criado e cartão adicionado para usuário {UserId}.", 
                    newCustomer.Id, userId);
            }
            else
            {
                // Usuário já tem CustomerId, apenas adiciona o cartão
                resultCard = await AddCardToCustomerAsync(user.CustomerId, cardToken);
                
                logger.LogInformation("Cartão adicionado ao Customer {CustomerId}.", user.CustomerId);
            }

            return new WalletCardDto
            {
                Id = resultCard.Id ?? "",
                LastFourDigits = resultCard.LastFourDigits ?? "****",
                ExpirationMonth = resultCard.ExpirationMonth ?? 0,
                ExpirationYear = resultCard.ExpirationYear ?? 0,
                PaymentMethodId = resultCard.PaymentMethod?.Id ?? "unknown",
                IsSubscriptionActiveCard = false,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao adicionar cartão para usuário {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Cria um Customer no Mercado Pago e adiciona um cartão.
    /// Usado internamente durante o processo de checkout.
    /// NÃO persiste no banco local (apenas na API do MP).
    /// </summary>
    public async Task<CustomerWithCardResponseDto> CreateCustomerWithCardAsync(
        string email,
        string name,
        string token
    )
    {
        // 1. Cria o Customer no MP
        var customer = await mpService.CreateCustomerAsync(email, name);

        // 2. Adiciona o Cartão ao Customer criado
        var card = await mpService.AddCardAsync(customer.Id, token);

        // 3. Monta o DTO de resposta composta
        var cardDto = new CardInCustomerResponseDto(
            card.Id,
            card.LastFourDigits,
            card.ExpirationMonth,
            card.ExpirationYear,
            new PaymentMethodDto(card.PaymentMethod?.Id, card.PaymentMethod?.Name)
        );

        return new CustomerWithCardResponseDto(customer.Id, customer.Email, cardDto);
    }

    /// <summary>
    /// Remove um cartão da carteira do usuário.
    /// Impede a remoção se o cartão estiver vinculado a uma assinatura ativa.
    /// NÃO precisa de UnitOfWork (apenas deleta na API do MP, sem atualizar banco local).
    /// </summary>
    public async Task RemoveCardFromWalletAsync(string userId, string cardId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.CustomerId))
            throw new ResourceNotFoundException("Carteira não encontrada.");

        // Validação de segurança: não permite remover cartão da assinatura ativa
        var activeSubscription = await subscriptionRepository.GetActiveSubscriptionByUserIdAsync(userId);
        if (activeSubscription != null && activeSubscription.CardTokenId == cardId)
        {
            throw new InvalidOperationException(
                "Este cartão está vinculado à sua assinatura ativa e não pode ser removido."
            );
        }

        await DeleteCardFromCustomerAsync(user.CustomerId, cardId);
        
        logger.LogInformation("Cartão {CardId} removido da carteira do usuário {UserId}.", cardId, userId);
    }

    // --- MÉTODOS PRIVADOS ---

    /// <summary>
    /// Adiciona um cartão a um Customer existente no Mercado Pago.
    /// Invalida o cache após a operação.
    /// </summary>
    public async Task<CardInCustomerResponseDto> AddCardToCustomerAsync(
        string customerId,
        string cardToken
    )
    {
        // 1. Adiciona cartão via SDK do Mercado Pago
        var mpCard = await mpService.AddCardAsync(customerId, cardToken);

        // 2. Limpa o cache para garantir consistência
        await cacheService.RemoveAsync($"customer-cards:{customerId}");
        
        var paymentMethodDto = new PaymentMethodDto(
            mpCard.PaymentMethod?.Id,
            mpCard.PaymentMethod?.Name
        );

        return new CardInCustomerResponseDto(
            mpCard.Id,
            mpCard.LastFourDigits,
            mpCard.ExpirationMonth,
            mpCard.ExpirationYear,
            paymentMethodDto
        );
    }

    /// <summary>
    /// Remove um cartão de um Customer no Mercado Pago.
    /// Invalida o cache após a operação.
    /// </summary>
    private async Task DeleteCardFromCustomerAsync(string customerId, string cardId)
    {
        await mpService.DeleteCardAsync(customerId, cardId);
        await cacheService.RemoveAsync($"customer-cards:{customerId}");
    }

    /// <summary>
    /// Lista todos os cartões de um Customer.
    /// Utiliza cache de 15 minutos para otimizar performance.
    /// </summary>
    private async Task<List<CardInCustomerResponseDto>> ListCardsFromCustomerAsync(
        string customerId
    )
    {
        var cacheKey = $"customer-cards:{customerId}";
        return await cacheService.GetOrCreateAsync(
                cacheKey,
                async () => await mpService.ListCardsAsync(customerId),
                TimeSpan.FromMinutes(15)
            ) ?? [];
    }
}
