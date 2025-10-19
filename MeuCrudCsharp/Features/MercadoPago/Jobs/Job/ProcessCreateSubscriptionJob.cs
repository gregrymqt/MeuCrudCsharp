using System;
using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Services;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Notification.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Utils;
using MeuCrudCsharp.Features.MercadoPago.Webhooks.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs.Job;

public class ProcessCreateSubscriptionJob : IJob<PaymentNotificationData>
{
    private readonly ILogger<ProcessCreateSubscriptionJob> _logger;
    private readonly ApiDbContext _context;
    private readonly CacheService _cache;
    private readonly ISubscriptionCreateNotificationService _notificationSubscriptionCreate;

    public ProcessCreateSubscriptionJob(
        ILogger<ProcessCreateSubscriptionJob> logger,
        ApiDbContext context,
        CacheService cache,
        ISubscriptionCreateNotificationService notificationSubscriptionCreate
    )
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _notificationSubscriptionCreate = notificationSubscriptionCreate;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 60 })]
    public async Task ExecuteAsync(PaymentNotificationData resource)
    {
        if (string.IsNullOrEmpty(resource?.Id))
        {
            _logger.LogError(
                "Job de notificação de pagamento recebido com um ResourceId nulo ou vazio. O job será descartado."
            );
            throw new ArgumentNullException(
                nameof(resource.Id),
                "O ID do recurso não pode ser nulo."
            );
        }

        _logger.LogInformation(
            "Iniciando processamento do job para a criação de assinatura: {ResourceId}",
            resource.Id
        );

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var assinaturaExistente = await _context
                .Subscriptions.FromSqlRaw(
                    "SELECT * FROM Subscriptions WITH (UPDLOCK, ROWLOCK) WHERE ExternalId = {0}",
                    resource.Id
                )
                .FirstOrDefaultAsync();
            if (assinaturaExistente == null)
            {
                throw new ResourceNotFoundException(
                    $"Assinatura com ID externo {resource.Id} não encontrada no banco. Tentando novamente mais tarde."
                );
            }

            var statusProcessaveis = new[]
            {
                InternalPaymentStatus.Pendente,
                InternalPaymentStatus.Iniciando,
            };
            if (!statusProcessaveis.Contains(assinaturaExistente.Status))
            {
                _logger.LogInformation(
                    "Assinatura {ResourceId} já foi processada (Status: {Status}). Finalizando job com sucesso.",
                    resource.Id,
                    assinaturaExistente.Status
                );
                await transaction.CommitAsync();
                return;
            }
            await _notificationSubscriptionCreate.VerifyAndProcessSubscriptionAsync(
                assinaturaExistente // Passando o ID interno
            );
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Processamento da criação de assinatura ID: {ResourceId} concluído com sucesso.",
                resource.Id
            );
            var cacheKey = $"subscription_{resource.Id}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation("Cache invalidado para a chave: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar a criação de assinatura ID: {ResourceId}",
                resource.Id
            );

            await transaction.RollbackAsync();
            throw;
        }
    }
}
