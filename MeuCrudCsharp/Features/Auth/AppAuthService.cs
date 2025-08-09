using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

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

        /// <summary>
        /// Inicializa o serviço de autenticação da aplicação.
        /// </summary>
        /// <param name="userManager">Gerenciador de usuários do ASP.NET Identity.</param>
        /// <param name="configuration">A configuração da aplicação (para acessar a chave JWT, por exemplo).</param>
        /// <param name="logger">Logger para registrar eventos de autenticação.</param>
        public AppAuthService(
            UserManager<Users> userManager,
            IConfiguration configuration,
            ILogger<AppAuthService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Realiza o login do usuário usando cookies e emite um token JWT armazenado em cookie HttpOnly.
        /// </summary>
        /// <param name="user">Usuário autenticado.</param>
        /// <param name="httpContext">Contexto HTTP atual.</param>
        public async Task SignInUser(Users user, HttpContext httpContext)
        {
            if (user == null)
            {
                throw new ArgumentNullException(
                    nameof(user),
                    "O objeto do usuário não pode ser nulo para o login."
                );
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(
                    nameof(httpContext),
                    "O HttpContext é necessário para o login."
                );
            }

            try
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty),
                };

                foreach (var role in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme
                );

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                };

                // Gera o token JWT
                var jwtToken = await GenerateJwtTokenAsync(user);

                // Realiza o login via cookie (Identity + Cookies)
                await httpContext.SignInAsync(
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                // Grava o JWT em um cookie HttpOnly para uso em APIs (lido pelo JwtBearer)
                httpContext.Response.Cookies.Append(
                    "jwt",
                    jwtToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = authProperties.ExpiresUtc
                    }
                );

                _logger.LogInformation(
                    "Usuário {UserId} - {UserEmail} logado com sucesso.",
                    user.Id,
                    user.Email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao tentar realizar o login para o usuário {UserId} - {UserEmail}.",
                    user.Id,
                    user.Email
                );

                throw new AppServiceException(
                    "Ocorreu um erro inesperado ao tentar realizar o login.",
                    ex
                );
            }
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
                throw new ArgumentNullException(nameof(user), "Usuário não pode ser nulo para gerar o token.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty)
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
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
    }
}
