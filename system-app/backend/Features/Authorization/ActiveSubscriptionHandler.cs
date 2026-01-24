using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Authorization;

public class ActiveSubscriptionHandler(IDbContextFactory<ApiDbContext> dbContextFactory)
    : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    // Use IDbContextFactory para evitar problemas de concorrência em handlers, que são singletons.

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSubscriptionRequirement requirement
    )
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return; // Sai do método, autorização concedida.
        }

        // 1. Obter o ID do usuário a partir do token JWT
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            // Se não há ID de usuário no token, a autorização falha.
            context.Fail();
            return;
        }

        // 2. Acessar o banco de dados para verificar a assinatura
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        // A consulta verifica se existe ALGUMA assinatura para este usuário com o status "ativo".
        // IMPORTANTE: Ajuste o valor "active" para o status exato que você usa no seu sistema
        // para assinaturas ativas (ex: "approved", "authorized", etc.).
        var hasActiveSubscription = await dbContext
            .Set<Subscription>()
            .AnyAsync(s => s.UserId == userId && s.Status == "ativo"); // <-- AJUSTE O STATUS AQUI

        if (hasActiveSubscription)
        {
            // Se o usuário tem uma assinatura ativa, o requisito é atendido.
            context.Succeed(requirement);
        }
        else
        {
            // Se não tiver, a autorização falha.
            context.Fail();
        }
    }
}
