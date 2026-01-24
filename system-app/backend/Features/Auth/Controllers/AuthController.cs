using System.IdentityModel.Tokens.Jwt;
using System.Security.Policy;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        SignInManager<Users> signInManager,
        IAppAuthService authService,
        IJwtService jwtService,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        ICacheService cacheService)
        : ApiControllerBase
    {
        /// <summary>
        /// 1. O React redireciona o usuário para cá para iniciar o login
        /// GET: api/auth/google-login
        /// </summary>
        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            // Define que, após o Google logar, ele deve chamar a nossa action 'GoogleCallback' abaixo
            var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth");

            var properties = signInManager.ConfigureExternalAuthenticationProperties(
                "Google",
                redirectUrl
            );

            // Isso lança o desafio (302 Redirect) para a URL do Google Accounts
            return new ChallengeResult("Google", properties);
        }

        /// <summary>
        /// 2. O Google traz o usuário de volta para cá
        /// </summary>
        [HttpGet("google-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback(string? remoteError = null)
        {
            // URL do seu Frontend (React)
            // Idealmente, coloque isso no appsettings.json como "FrontendUrl"
            var frontendUrl = configuration["General:BaseUrl"] ?? "http://localhost:5173";
            var frontendCallbackUrl = $"{frontendUrl}/google-callback";

            if (remoteError != null)
            {
                logger.LogError("Erro do provedor externo: {RemoteError}", remoteError);
                return Redirect($"{frontendUrl}/login?error=ExternalProviderError");
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                logger.LogError("Erro ao carregar informações de login externo.");
                return Redirect($"{frontendUrl}/login?error=NoExternalInfo");
            }

            try
            {
                // REUTILIZANDO SUA LÓGICA EXISTENTE
                // O método SignInWithGoogleAsync já cria o usuário, faz updates e gera o cookie se necessário
                var user = await authService.SignInWithGoogleAsync(info.Principal, HttpContext);

                // REUTILIZANDO SEU SERVIÇO DE JWT [cite: 56, 62]
                var tokenString = await jwtService.GenerateJwtTokenAsync(user);

                // Limpa o cookie temporário do Identity External
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                // REDIRECIONAMENTO FINAL:
                // Mandamos o usuário de volta para o React com o Token na Query String
                return Redirect($"{frontendCallbackUrl}?token={tokenString}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no fluxo de login Google.");
                return Redirect($"{frontendUrl}/login?error=ServerException");
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var userId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (string.IsNullOrEmpty(userId)) // [cite: 33]
                    return Unauthorized("Token inválido.");

                // 2. Chama o serviço otimizado
                // O retorno agora é o DTO leve com booleanos
                var userSession = await authService.GetAuthenticatedUserDataAsync(userId);

                return Ok(userSession); // [cite: 35]
            }
            catch (ResourceNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao recuperar dados do usuário.");
                return StatusCode(500, "Erro interno ao obter dados do usuário.");
            }
        }

        /// <summary>
        /// Realiza o Logout e invalida o token JWT atual adicionando-o à Blacklist do Redis.
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 1. Pega o Token do Header Authorization
                var token = HttpContext
                    .Request.Headers.Authorization.ToString()
                    .Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                    return BadRequest("Token não encontrado.");

                // 2. Lê o tempo de expiração do Token (exp)
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Calcula quanto tempo falta para o token morrer naturalmente
                var expiration = jwtToken.ValidTo;
                var now = DateTime.UtcNow;
                var ttl = expiration - now;

                // 3. Se o token ainda não venceu, adiciona na Blacklist do Redis
                // Usamos o TTL exato, assim o Redis limpa a memória sozinho quando o token expirar de verdade
                if (ttl.TotalSeconds > 0)
                {
                    // A chave será "blacklist:eyJhbGci..."
                    await cacheService.GetOrCreateAsync<string>(
                        $"blacklist:{token}",
                        () => Task.FromResult("revoked"), // Valor dummy
                        ttl // Tempo de vida exato
                    );

                    logger.LogInformation(
                        "Token invalidado e adicionado à blacklist por {TtlTotalMinutes} minutos.", ttl.TotalMinutes
                    );
                }

                // 4. Logout do Identity (Cookie) caso esteja usando cookie híbrido
                await signInManager.SignOutAsync();

                return Ok(new { message = "Logout realizado com sucesso." });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar logout.");
                return BadRequest("Erro ao processar logout.");
            }
        }
    }
}
