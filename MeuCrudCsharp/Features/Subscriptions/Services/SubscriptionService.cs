// Local: Features/Subscriptions/Services/SubscriptionService.cs

using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Clients.Interfaces; // CORREÇÃO: Usar a interface do ClientService
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.Subscriptions.DTOs;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Subscriptions.Services
{
    public class SubscriptionService : MercadoPagoServiceBase, ISubscriptionService
    {
        // --- CORREÇÃO: Declarar todas as dependências como campos privados ---
        private readonly ApiDbContext _context;
        private readonly IClientService _clientService; // Dependência para criar clientes e cartões
        private readonly ICacheService _cacheService; // Descomente se você tiver um serviço de cache

        // --- CORREÇÃO: Injetar todas as dependências e corrigir o tipo do Logger ---
        public SubscriptionService(
            HttpClient httpClient,
            ILogger<SubscriptionService> logger, // O Logger deve ser do tipo da própria classe
            ApiDbContext context,
            IClientService clientService,
            ICacheService cacheService
        )
            : base(httpClient, logger)
        {
            _context = context;
            _clientService = clientService;
            _cacheService = cacheService;
        }

        public async Task<SubscriptionResponseDto> CreateSubscriptionAndCustomerIfNeededAsync(
            CreateSubscriptionDto createDto,
            ClaimsPrincipal users
        )
        {
            var userIdString = users.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdString);
            if (user == null)
            {
                throw new AppServiceException("Usuário não encontrado.");
            }

            string customerId = user.MercadoPagoCustomerId;

            if (string.IsNullOrEmpty(customerId))
            {
                _logger.LogInformation(
                    "Usuário {UserId} não possui um cliente no MP. Criando agora...",
                    userIdString
                );

                // --- CORREÇÃO: Usar o _clientService injetado ---
                var newCustomer = await _clientService.CreateCustomerAsync(user.Email, user.Name);
                customerId = newCustomer.Id;
                user.MercadoPagoCustomerId = customerId;
            }

            // --- CORREÇÃO: Usar o _clientService injetado ---
            var savedCard = await _clientService.AddCardToCustomerAsync(
                customerId,
                createDto.CardTokenId
            );

            // A lógica de criar a assinatura em si já estava quase certa, agora ela é um método separado.
            var subscriptionResponse = await CreateSubscriptionAsync(
                createDto.PreapprovalPlanId,
                savedCard.Id,
                createDto.PayerEmail
            );

            var localPlan = await _context
                .Plans.AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ExternalPlanId == subscriptionResponse.PreapprovalPlanId
                );

            if (localPlan == null)
            {
                throw new ResourceNotFoundException(
                    $"Plano com ID externo '{subscriptionResponse.PreapprovalPlanId}' não encontrado."
                );
            }
            if (!Guid.TryParse(userIdString, out var userIdGuid))
            {
                throw new AppServiceException("ID de usuário inválido na sessão.");
            }

            var newSubscription = new Subscription
            {
                UserId = userIdGuid,
                PlanId = localPlan.Id,
                ExternalId = subscriptionResponse.Id,
                Status = subscriptionResponse.Status,
                PayerEmail = subscriptionResponse.PayerEmail,
                CreatedAt = DateTime.UtcNow,
                LastFourCardDigits = savedCard.LastFourDigits,
            };

            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Assinatura {SubscriptionId} criada com sucesso para o usuário {UserId}",
                newSubscription.ExternalId,
                userIdString
            );
            return subscriptionResponse;
        }

        // --- CORREÇÃO: Métodos que interagem com a API agora vivem aqui, usando o método base ---
        public async Task<SubscriptionResponseDto> GetSubscriptionByIdAsync(string subscriptionId)
        {
            var endpoint = $"/preapproval/{subscriptionId}";
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Get,
                endpoint,
                (object?)null
            );
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException("Falha ao desserializar dados da assinatura.");
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionCardAsync(
            string subscriptionId,
            string newCardId
        )
        {
            var endpoint = $"/v1/preapproval/{subscriptionId}";
            var payload = new SubscriptionWithCardRequestDto { CardId = newCardId };
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta da atualização da assinatura."
                );
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionValueAsync(
            string subscriptionId,
            UpdateSubscriptionValueDto dto
        )
        {
            _logger.LogInformation(
                "Iniciando atualização de valor para a assinatura MP: {SubscriptionId}",
                subscriptionId
            );

            // 1. Monta o endpoint da API
            var endpoint = $"/v1/preapproval/{subscriptionId}";

            // 2. Cria o payload no formato que a API do Mercado Pago espera
            var payload = new UpdateSubscriptionValueDto
            {
                TransactionAmount = dto.TransactionAmount,
            };

            // 3. Chama o método genérico da classe base para enviar a requisição
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            var mpSubscriptionResponse =
                JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta da atualização da assinatura."
                );

            // 4. Sincroniza a mudança com o banco de dados local (Passo Opcional, mas recomendado)
            var localSubscription = await _context
                .Subscriptions.Include(s => s.Plan) // Inclui o plano para poder alterar o valor
                .FirstOrDefaultAsync(s => s.ExternalId == subscriptionId);

            if (localSubscription?.Plan != null)
            {
                localSubscription.Plan.TransactionAmount = dto.TransactionAmount;
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Valor do plano local associado à assinatura {SubscriptionId} foi atualizado.",
                    subscriptionId
                );
            }
            else
            {
                _logger.LogWarning(
                    "Assinatura {SubscriptionId} atualizada no MP, mas plano local não foi encontrado para sincronização.",
                    subscriptionId
                );
            }

            await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");

            return mpSubscriptionResponse;
        }

        public async Task<SubscriptionResponseDto> UpdateSubscriptionStatusAsync(
            string subscriptionId,
            UpdateSubscriptionStatusDto dto
        )
        {
            var endpoint = $"/v1/preapproval/{subscriptionId}";
            var payload = new UpdateSubscriptionStatusDto { Status = dto.Status };
            var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
            var mpSubscriptionResponse =
                JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta da atualização de status."
                );

            var localSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s =>
                s.ExternalId == subscriptionId
            );

            if (localSubscription != null)
            {
                localSubscription.Status = dto.Status; // Ex: "cancelled" ou "paused"
                localSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _cacheService.RemoveAsync($"SubscriptionDetails_{localSubscription.UserId}");
                _logger.LogInformation(
                    "Status da assinatura {SubscriptionId} atualizado para {Status} no banco de dados local.",
                    subscriptionId,
                    dto.Status
                );
            }
            else
            {
                _logger.LogWarning(
                    "Assinatura {SubscriptionId} foi atualizada no Mercado Pago, mas não foi encontrada no banco de dados local para sincronização.",
                    subscriptionId
                );
            }
            return mpSubscriptionResponse;
        }

        // Método privado para a lógica de criação da assinatura, mantendo o código limpo
        private async Task<SubscriptionResponseDto> CreateSubscriptionAsync(
            string preapprovalPlanId,
            string cardId,
            string payerEmail
        )
        {
            const string endpoint = "/preapproval";
            var payload = new SubscriptionWithCardRequestDto
            {
                PreapprovalPlanId = preapprovalPlanId,
                CardId = cardId,
                Payer = new PayerRequestDto { Email = payerEmail },
            };
            var responseBody = await SendMercadoPagoRequestAsync(
                HttpMethod.Post,
                endpoint,
                payload
            );
            return JsonSerializer.Deserialize<SubscriptionResponseDto>(responseBody)
                ?? throw new AppServiceException(
                    "Falha ao desserializar a resposta da criação da assinatura."
                );
        }
    }
}
