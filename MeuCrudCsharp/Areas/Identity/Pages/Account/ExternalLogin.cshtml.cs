using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MeuCrudCsharp.Features.Auth;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IAppAuthService _authService;

        public ExternalLoginModel(
            UserManager<Users> userManager,
            SignInManager<Users> signInManager,
            ILogger<ExternalLoginModel> logger,
            IAppAuthService appAuthService
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _authService = appAuthService;
        }

        // ✅ PROPRIEDADES QUE ESTAVAM FALTANDO, AGORA NO LUGAR CERTO
        [BindProperty]
        public InputModel Input { get; set; } = new();
        public string? ProviderDisplayName { get; set; }
        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        // Este método apenas redireciona se alguém tentar acessar a página diretamente.
        public IActionResult OnGet() => RedirectToPage("./Login");

        // Este é o método que o seu AccountController chama para iniciar o fluxo.
        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl }
            );
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
                provider,
                redirectUrl
            );
            return new ChallengeResult(provider, properties);
        }

        // Este é o método que o ASP.NET Core Identity chama após o retorno do Google.
        public async Task<IActionResult> OnGetCallbackAsync(
            string? returnUrl = null,
            string? remoteError = null
        )
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Erro do provedor externo: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Erro ao carregar informações de login externo.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            try
            {
                // ✅ AQUI ESTÁ A MUDANÇA:
                // Apenas chamamos nosso serviço para fazer todo o trabalho.
                var user = await _authService.SignInWithGoogleAsync(info.Principal, HttpContext);

                _logger.LogInformation(
                    "Usuário {UserId} processado com sucesso pelo serviço via {LoginProvider}.",
                    user.Id,
                    info.LoginProvider
                );

                // Limpa o cookie externo temporário
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                // E redireciona o usuário.
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ocorreu um erro durante o processo de SignInWithGoogleAsync."
                );
                ErrorMessage = "Ocorreu um erro inesperado durante o login. Tente novamente.";
                return RedirectToPage("./Login");
            }
        }
    }
}
