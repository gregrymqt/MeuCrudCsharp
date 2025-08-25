using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Exceptions; // Importando nossas exceções customizadas
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // MUDANÇA 1: Adicionando o Logger

namespace MeuCrudCsharp.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ILogger<IndexModel> _logger; // MUDANÇA 1

        public ProfileViewModel ViewModel { get; private set; } = new(); // Inicializa para evitar nulos

        public IndexModel(IUserAccountService userAccountService, ILogger<IndexModel> logger) // MUDANÇA 1
        {
            _userAccountService = userAccountService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                _logger.LogWarning(
                    "Tentativa de acesso à página de perfil com um ID de usuário inválido no cookie."
                );
                return Unauthorized();
            }

            try
            {
                // Bloco CORRIGIDO e SEGURO
                var userProfile = await _userAccountService.GetUserProfileAsync(userIdString);
                var subscription = await _userAccountService.GetUserSubscriptionDetailsAsync(
                    userIdString
                );
                var paymentHistory = await _userAccountService.GetUserPaymentHistoryAsync(
                    userIdString
                );

                ViewModel = new ProfileViewModel
                {
                    UserProfile = userProfile,
                    Subscription = subscription,
                    PaymentHistory = paymentHistory,
                };

                return Page();
            }
            // MUDANÇA 3: Tratamento de exceções mais específico e robusto
            catch (ResourceNotFoundException ex)
            {
                // Este erro é crítico: o usuário está logado, mas não existe no banco.
                _logger.LogError(
                    ex,
                    "Usuário autenticado com ID {UserId} não foi encontrado no banco de dados.",
                    userIdString
                );
                // Deslogar o usuário pode ser uma boa ação aqui para limpar o cookie inválido.
                await HttpContext.SignOutAsync();
                return NotFound(
                    "Seu usuário não foi encontrado em nosso sistema. Por favor, contate o suporte."
                );
            }
            catch (ExternalApiException ex)
            {
                // Aconteceu um erro ao falar com o Mercado Pago. A página ainda pode ser útil.
                _logger.LogWarning(
                    ex,
                    "Falha na API externa ao carregar dados para o usuário {UserId}.",
                    userIdString
                );
                TempData["ErrorMessage"] =
                    "Não foi possível carregar os detalhes da sua assinatura no momento. Tente novamente mais tarde.";

                // Tenta carregar os dados que não dependem da API externa para uma experiência degradada
                await LoadPartialViewModelOnError(userIdString);
                return Page();
            }
            catch (Exception ex)
            {
                // Erro genérico (ex: banco de dados fora do ar)
                _logger.LogError(
                    ex,
                    "Erro inesperado ao carregar a página de perfil para o usuário {UserId}.",
                    userIdString
                );
                TempData["ErrorMessage"] =
                    "Ocorreu um erro ao carregar seus dados. Nossa equipe já foi notificada.";

                // Tenta carregar o mínimo de dados possível
                await LoadPartialViewModelOnError(userIdString);
                return Page();
            }
        }

        /// <summary>
        /// Método auxiliar para carregar dados parciais quando uma parte da página falha.
        /// </summary>
        private async Task LoadPartialViewModelOnError(string userIdString)
        {
            try
            {
                // Garante que pelo menos o perfil do usuário seja carregado, se possível.
                if (ViewModel.UserProfile == null)
                {
                    ViewModel.UserProfile = await _userAccountService.GetUserProfileAsync(
                        userIdString
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao carregar o ViewModel parcial para o usuário {UserId} após erro inicial.",
                    userIdString
                );
                // Se até o perfil falhar, a página ficará com os dados padrão.
            }
        }
    }
}
