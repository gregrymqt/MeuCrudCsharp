using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces; // MUDANÇA 1: Usando a interface do serviço
using MeuCrudCsharp.Features.Profiles.UserAccount.ViewModels; // MUDANÇA 2: Usando o ViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        // MUDANÇA 3: Injetando o SERVIÇO, não mais o DbContext diretamente.
        private readonly IUserAccountService _userAccountService;

        // MUDANÇA 4: A página agora terá uma única propriedade, o ViewModel, que contém todos os dados.
        public ProfileViewModel? ViewModel { get; private set; }

        public IndexModel(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        // MUDANÇA 5: Lógica do OnGetAsync totalmente refatorada para usar o serviço.
        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdString, out var userId))
            {
                // Este erro indica um problema com o cookie de autenticação.
                TempData["ErrorMessage"] = "ID de usuário inválido.";
                return Unauthorized();
            }

            try
            {
                var userProfileDto = await _userAccountService.GetUserProfileAsync(userId);
                var subscriptionDetailsDto =
                    await _userAccountService.GetUserSubscriptionDetailsAsync(userId);
                var paymentHistory = await _userAccountService.GetUserPaymentHistoryAsync(userId); // NOVO

                ViewModel = new ProfileViewModel
                {
                    UserProfile = userProfileDto,
                    Subscription = subscriptionDetailsDto,
                    PaymentHistory = paymentHistory, // NOVO
                };

                return Page();
            }
            catch (KeyNotFoundException ex)
            {
                // Erro caso o serviço não encontre o usuário (ex: foi deletado mas o cookie ainda existe)
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Em um app real, logar o erro `ex` é crucial.
                // Poderia ser um erro de comunicação com a API do Mercado Pago, por exemplo.
                // Adiciona uma mensagem de erro para ser exibida na página.
                TempData["ErrorMessage"] =
                    "Não foi possível carregar os detalhes da sua assinatura. Tente novamente mais tarde.";

                // Mesmo com erro na assinatura, ainda tentamos carregar o perfil básico.
                // Se isso também falhar, o catch acima pegaria.
                if (ViewModel?.UserProfile == null)
                {
                    var userProfileDto = await _userAccountService.GetUserProfileAsync(userId);
                    ViewModel = new ProfileViewModel { UserProfile = userProfileDto };
                }

                return Page();
            }
        }
    }
}
