using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;

namespace MeuCrudCsharp.Features.Auth.Services
{
    public class AuthService : IAppAuthService
    {
        private readonly UserManager<Users> _userManager;
        private readonly IUserRepository _userRepository; // <-- Nova dependência
        private readonly IJwtService _jwtService; // <-- Nova dependência
        private readonly ILogger<AuthService> _logger;

        // O DbContext foi REMOVIDO daqui!
        public AuthService(
            UserManager<Users> userManager,
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<AuthService> logger
        )
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
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
                throw new InvalidOperationException(
                    "Não foi possível obter os dados do provedor externo."
                );
            }

            // CORREÇÃO: Acesso ao banco via repositório.
            var user = await _userRepository.FindByGoogleIdAsync(googleId);

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    // ... Lógica para criar o novo usuário ...
                    // (O código aqui dentro permanece o mesmo, pois já usa o _userManager)
                    // --- INÍCIO DO CÓDIGO INALTERADO ---
                    _logger.LogInformation(
                        "Nenhum usuário encontrado para {Email}. Criando uma nova conta.",
                        email
                    );
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
                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Não foi possível criar o usuário: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                        );
                    }

                    if (
                        user.Email.Equals(
                            "lucasvicentedesouza021@gmail.com",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                    // --- FIM DO CÓDIGO INALTERADO ---
                }

                await _userManager.AddLoginAsync(
                    user,
                    new UserLoginInfo("Google", googleId, "Google")
                );
            }

            // CORREÇÃO: Geração de token delegada para o JwtService.
            var jwtString = await _jwtService.GenerateJwtTokenAsync(user);

            // A lógica de adicionar o cookie permanece aqui, pois faz parte do processo de "autenticação".
            httpContext.Response.Cookies.Append(
                "jwt",
                jwtString,
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.UtcNow.AddHours(7),
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                }
            );

            _logger.LogInformation("Usuário {UserId} logado com sucesso via Google.", user.Id);
            return user;
        }
    }
}
