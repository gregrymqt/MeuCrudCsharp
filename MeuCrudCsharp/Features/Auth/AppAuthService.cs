using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MeuCrudCsharp.Features.Auth
{
    /// <summary>
    /// Serviço responsável por autenticar usuários, gerar tokens JWT
    /// e realizar o login via cookies.
    /// </summary>
    public class AppAuthService : IAppAuthService
    {
        private readonly ILogger<AppAuthService> _logger;
        private readonly UserManager<Users> _userManager;
        private readonly IConfiguration _configuration;

        private readonly ApiDbContext _dbContext;

        /// <summary>
        /// Inicializa o serviço de autenticação da aplicação.
        /// </summary>
        /// <param name="userManager">Gerenciador de usuários do ASP.NET Identity.</param>
        /// <param name="configuration">A configuração da aplicação (para acessar a chave JWT, por exemplo).</param>
        /// <param name="logger">Logger para registrar eventos de autenticação.</param>
        public AppAuthService(
            UserManager<Users> userManager,
            IConfiguration configuration,
            ILogger<AppAuthService> logger,
            ApiDbContext dbContext
        )
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Gera um token JWT para o usuário informado, incluindo suas roles como claims.
        /// </summary>
        /// <param name="user">Usuário para o qual o token será gerado.</param>
        /// <returns>String do token JWT assinado.</returns>
        public async Task<string> GenerateJwtTokenAsync(Users user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(
                    nameof(user),
                    "Usuário não pode ser nulo para gerar o token."
                );
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            _logger.LogInformation("Token JWT gerado para o usuário {UserId}", user.Id);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<Users> SignInWithGoogleAsync(
            ClaimsPrincipal googleUserPrincipal,
            HttpContext httpContext
        )
        {
            string? googleId = googleUserPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            string? email = googleUserPrincipal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning(
                    "Tentativa de login com Google falhou: GoogleId ou Email não encontrados."
                );
                // Lança uma exceção para ser tratada pelo PageModel
                throw new InvalidOperationException(
                    "Não foi possível obter os dados do provedor externo."
                );
            }

            // Procura o usuário
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
            if (user == null)
            {
                // Se não encontra, procura por e-mail para vincular a conta
                user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // Se realmente não existe, cria um novo
                    string? name = googleUserPrincipal.FindFirstValue(ClaimTypes.Name);
                    string? avatar = googleUserPrincipal.FindFirstValue("urn:google:picture");
                    user = new Users
                    {
                        UserName = email,
                        Email = email,
                        GoogleId = googleId,
                        Name = name ?? "Usuário",
                        AvatarUrl = avatar ?? string.Empty,
                        EmailConfirmed = true,
                    };
                    await _userManager.CreateAsync(user);
                }
                // Associa o login do Google à conta encontrada ou recém-criada
                await _userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo("Google", googleId, "Google")
                );
            }

            // Gera o token JWT
            var jwtString = await GenerateJwtTokenAsync(user);

            // Adiciona o cookie JWT à resposta
            httpContext.Response.Cookies.Append(
                "jwt",
                jwtString,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(8),
                }
            );

            _logger.LogInformation("Usuário {UserId} logado com sucesso via Google.", user.Id);

            // Retorna o usuário processado
            return user;
        }
    }
}
