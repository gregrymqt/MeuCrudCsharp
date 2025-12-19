using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Dtos;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
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
            var user = await _userRepository.FindByGoogleIdAsync(googleId);

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
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

        public async Task<UserSessionDto> GetAuthenticatedUserDataAsync(string userId)
        {
            // 1. Busca os dados completos no banco
            var user = await _userRepository.GetUserWithDetailsAsync(userId);

            if (user == null)
                throw new ResourceNotFoundException("Usuário não encontrado."); // [cite: 68]

            // 2. Mapeia para o DTO (UserSessionDto)
            var sessionDto = new UserSessionDto
            {
                PublicId = user.PublicId, // [cite: 42]
                Name = user.Name ?? "Usuário",
                Email = user.Email, // Herdado de IdentityUser
                AvatarUrl = user.AvatarUrl, // [cite: 43]
                LastPayments = user
                    .Payments.Select(p => new PaymentHistoryDto
                    {
                        Amount = p.Amount, // [cite: 55]
                        DateApproved = p.DateApproved, // [cite: 52]
                        Status = p.Status.ToString(), // Do TransactionBase
                        Method = p.Method, // [cite: 50]
                        LastFourDigits = p.LastFourDigits, // [cite: 53]
                    })
                    .ToList(),
            };

            // 3. Mapeia a Assinatura se existir
            if (user.Subscription != null)
            {
                // Lógica simples para determinar se está ativo (data futura e status paid/active)
                bool isActive =
                    user.Subscription.CurrentPeriodEndDate > DateTime.UtcNow
                    && user.Subscription.Status == "paid"; // Ajuste conforme seu Enum de Status

                sessionDto.Subscription = new SubscriptionDto
                {
                    Status = user.Subscription.Status,
                    PlanName = user.Subscription.Plan?.Name ?? "Plano Desconhecido", // [cite: 61]
                    Price = user.Subscription.Plan.TransactionAmount, // Herdado de TransactionBase
                    StartDate = user.Subscription.CurrentPeriodStartDate, // [cite: 64]
                    EndDate = user.Subscription.CurrentPeriodEndDate, // [cite: 65]
                    IsActive = isActive,
                };
            }

            return sessionDto;
        }
    }
}
