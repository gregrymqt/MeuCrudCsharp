using System;
using MeuCrudCsharp.Features.Caching.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MeuCrudCsharp.Features.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute : ActionFilterAttribute
{
    private readonly int _limit;
    private readonly int _seconds;

    // Ex: [RateLimit(5, 60)] = Máximo 5 chamadas a cada 60 segundos
    public RateLimitAttribute(int limit, int seconds)
    {
        _limit = limit;
        _seconds = seconds;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Pega o User ID do JWT (Claims)
        var userId = context.HttpContext.User.FindFirst("id")?.Value; // ou ClaimTypes.NameIdentifier
        
        if (userId == null) 
        {
            await next(); // Se não tem user, segue (ou retorna 401 dependendo da regra)
            return;
        }

        // 2. Resolve o CacheService manualmente (atributos não têm injeção de construtor direta fácil)
        var cacheService = context.HttpContext.RequestServices.GetService<ICacheService>();
        
        // 3. Chave única: rate_limit:USER_ID:ENDPOINT_NAME
        var key = $"rate_limit:{userId}:{context.ActionDescriptor.DisplayName}";

        // 4. Incrementa contador no Redis
        // Precisamos adicionar um método IncrementAsync no seu CacheService para isso ser atômico
        var count = await cacheService.IncrementAsync(key, _seconds); 

        if (count > _limit)
        {
            context.Result = new ContentResult
            {
                StatusCode = 429, // Too Many Requests
                Content = $"Você excedeu o limite de requisições. Tente novamente em alguns segundos."
            };
            return;
        }

        await next();
    }
}
