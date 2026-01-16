using System.Net;
using MeuCrudCsharp.Features.Caching.Interfaces; // Ou o namespace do seu ICacheService

namespace MeuCrudCsharp.Features.Auth.Middlewares
{
    public class JwtBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICacheService cacheService)
        {
            // 1. Tenta pegar o token do Header
            var token = context
                .Request.Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ")
                .Last();

            if (!string.IsNullOrEmpty(token))
            {
                // 2. Verifica se o token está na Blacklist do Redis
                // A chave deve ser idêntica a usada no Logout do AuthController: $"blacklist:{token}"
                var isRevoked = await cacheService.GetAsync<string>($"blacklist:{token}");

                if (!string.IsNullOrEmpty(isRevoked))
                {
                    // 3. Se estiver na lista negra, retorna 401 e CORTA a requisição aqui
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsJsonAsync(
                        new { message = "Token revogado. Faça login novamente." }
                    );
                    return;
                }
            }

            // 4. Se não tiver token ou não estiver na blacklist, segue o fluxo normal
            await _next(context);
        }
    }
}
