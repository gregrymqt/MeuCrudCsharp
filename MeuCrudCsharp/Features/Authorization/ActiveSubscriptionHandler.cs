using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.Authorization;

public class ActiveSubscriptionHandler : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    private readonly IDbContextFactory<ApiDbContext> _dbContextFactory;

    // Use IDbContextFactory para evitar problemas de concorrência em handlers, que são singletons.
    public ActiveSubscriptionHandler(IDbContextFactory<ApiDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveSubscriptionRequirement requirement)
    {
        // 1. Obter o ID do usuário a partir do token JWT
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            // Se não há ID de usuário no token, a autorização falha.
            context.Fail();
            return;
        }

        // 2. Acessar o banco de dados para verificar a assinatura
        using (var dbContext = _dbContextFactory.CreateDbContext())
        {
            // A consulta verifica se existe ALGUMA assinatura para este usuário com o status "ativo".
            // IMPORTANTE: Ajuste o valor "active" para o status exato que você usa no seu sistema
            // para assinaturas ativas (ex: "approved", "authorized", etc.).
            bool hasActiveSubscription = await dbContext.Set<Subscription>()
                .AnyAsync(s => s.UserId == userId && s.Status == "active"); // <-- AJUSTE O STATUS AQUI

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
}
