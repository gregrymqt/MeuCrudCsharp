using System.Security.Claims;
using MeuCrudCsharp.Features.Auth.Dtos;
using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;

namespace MeuCrudCsharp.Features.Auth.Services
{
    public class AuthService : IAppAuthService
    {
        private readonly UserManager<Users> _userManager;
        private readonly IUserRepository _userRepository; // <-- Nova dependência
        private readonly IUserRoleRepository _userRoleRepository; // <-- Nova dependência
        private readonly IJwtService _jwtService; // <-- Nova dependência
        private readonly ILogger<AuthService> _logger;
        private readonly IPaymentRepository _paymentRepository; // <-- Nova dependência
        private readonly ISubscriptionRepository _subscriptionRepository; // <-- Nova dependência

        // O DbContext foi REMOVIDO daqui!
        public AuthService(
            UserManager<Users> userManager,
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<AuthService> logger,
            IPaymentRepository paymentRepository,
            ISubscriptionRepository subscriptionRepository,
            IUserRoleRepository userRoleRepository
        )
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _userRoleRepository = userRoleRepository;
            _paymentRepository = paymentRepository;
            _subscriptionRepository = subscriptionRepository;
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
            // 1. Busca dados básicos do usuário [cite: 1]
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null) // [cite: 2]
                throw new ResourceNotFoundException("Usuário não encontrado.");

            // 2. Consultas Paralelas (Opcional, mas melhora performance)
            // Disparamos as 3 consultas ao mesmo tempo para o banco
            var paymentTask = _paymentRepository.HasAnyPaymentByUserIdAsync(userId);
            var subTask = _subscriptionRepository.HasActiveSubscriptionByUserIdAsync(userId);
            var rolesTask = _userRoleRepository.GetRolesByUserIdAsync(userId); // Nova task

            // Aguardamos todas terminarem
            await Task.WhenAll(paymentTask, subTask, rolesTask);

            // 3. Monta o DTO com os resultados
            return new UserSessionDto
            {
                PublicId = user.PublicId, // [cite: 3]
                Name = user.Name ?? "Usuário", // [cite: 5, 6]
                Email = user.Email, // [cite: 6]
                AvatarUrl = user.AvatarUrl, // [cite: 6]

                HasPaymentHistory = paymentTask.Result, // [cite: 3]
                HasActiveSubscription = subTask.Result, // [cite: 4]

                // 4. Preenche as Roles
                Roles = rolesTask.Result ?? new List<string>()
            };
        }
    }
}
