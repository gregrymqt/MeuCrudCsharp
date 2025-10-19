using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace MeuCrudCsharp.Features.Authorization;

public class SubscriptionAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    // Vamos usar a implementação padrão como base
    private readonly IAuthorizationMiddlewareResultHandler _defaultHandler =
        new AuthorizationMiddlewareResultHandler();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        // 1. Verificamos se a autorização FALHOU e se foi por "Acesso Negado" (Forbidden)
        if (authorizeResult.Forbidden)
        {
            // 2. Verificamos se a falha foi causada ESPECIFICAMENTE pelo nosso requisito de assinatura
            // Isso evita que a gente redirecione em outros erros de autorização (ex: roles)
            if (
                authorizeResult.AuthorizationFailure!.FailedRequirements.Any(req =>
                    req is ActiveSubscriptionRequirement
                )
            )
            {
                // ❗️ LÓGICA IMPORTANTE: API vs. NAVEGADOR
                // Se for uma chamada de API (ex: de um frontend JavaScript), um redirect não é ideal.
                // Verificamos o header 'Accept' para decidir o que fazer.
                var isApiCall = context
                    .Request.Headers["Accept"]
                    .ToString()
                    .Contains("application/json");

                if (isApiCall)
                {
                    // Para APIs, retornamos um JSON customizado que o frontend pode usar para redirecionar.
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(
                        new
                        {
                            error = "active_subscription_required",
                            message = "Uma assinatura ativa é necessária para acessar este recurso.",
                            redirectUrl = "/Payment/CreditCard", // O frontend usará essa URL
                        }
                    );
                    return; // Importante: Paramos a execução aqui.
                }
                else
                {
                    // Para navegação normal (ex: MVC, Razor Pages), fazemos o redirect.
                    context.Response.Redirect("/Payment/CreditCard"); // <-- SEU REDIRECIONAMENTO AQUI
                    return; // Importante: Paramos a execução aqui.
                }
            }
        }

        // Se não for o nosso caso específico, deixa o handler padrão do ASP.NET Core cuidar do resto.
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
