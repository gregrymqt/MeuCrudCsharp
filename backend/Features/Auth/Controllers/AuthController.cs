using System.IdentityModel.Tokens.Jwt;
using System.Security.Policy;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Caching.Interfaces;
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
    public class AuthController : ApiControllerBase
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly IAppAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public AuthController(
            SignInManager<Users> signInManager,
            IAppAuthService authService,
            IJwtService jwtService,
            ILogger<AuthController> logger,
            IConfiguration configuration,
            ICacheService cacheService
        )
        {
            _signInManager = signInManager;
            _authService = authService;
            _jwtService = jwtService;
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
        }

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
            
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            
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
            var frontendUrl = _configuration["General:BaseUrl"] ?? "http://localhost:5173"; 
            var frontendCallbackUrl = $"{frontendUrl}/google-callback";

            if (remoteError != null)
            {
                _logger.LogError($"Erro do provedor externo: {remoteError}");
                return Redirect($"{frontendUrl}/login?error=ExternalProviderError");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Erro ao carregar informações de login externo.");
                return Redirect($"{frontendUrl}/login?error=NoExternalInfo");
            }

            try
            {
                // REUTILIZANDO SUA LÓGICA EXISTENTE 
                // O método SignInWithGoogleAsync já cria o usuário, faz updates e gera o cookie se necessário
                var user = await _authService.SignInWithGoogleAsync(info.Principal, HttpContext);

                // REUTILIZANDO SEU SERVIÇO DE JWT [cite: 56, 62]
                var tokenString = await _jwtService.GenerateJwtTokenAsync(user);

                // Limpa o cookie temporário do Identity External
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                // REDIRECIONAMENTO FINAL:
                // Mandamos o usuário de volta para o React com o Token na Query String
                return Redirect($"{frontendCallbackUrl}?token={tokenString}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no fluxo de login Google.");
                return Redirect($"{frontendUrl}/login?error=ServerException");
            }
        }

        /// <summary>
        /// Realiza o Logout e invalida o token JWT atual adicionando-o à Blacklist do Redis.
        /// </summary>
        [HttpPost("logout")]
        [Authorize] // Só quem tem token válido pode pedir logout
        public async Task<IActionResult> Logout()
        {
            try
            {
                // 1. Pega o Token do Header Authorization
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                
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
                    await _cacheService.GetOrCreateAsync<string>(
                        $"blacklist:{token}", 
                        () => Task.FromResult("revoked"), // Valor dummy
                        ttl // Tempo de vida exato
                    );
                    
                    _logger.LogInformation($"Token invalidado e adicionado à blacklist por {ttl.TotalMinutes} minutos.");
                }

                // 4. Logout do Identity (Cookie) caso esteja usando cookie híbrido
                await _signInManager.SignOutAsync();

                return Ok(new { message = "Logout realizado com sucesso." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar logout.");
                return BadRequest("Erro ao processar logout.");
            }
        }
    }
}