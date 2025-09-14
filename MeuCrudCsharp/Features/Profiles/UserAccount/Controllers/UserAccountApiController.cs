using System.Security.Claims;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.UserAccount.Controllers;

[ApiController]
[Authorize]
[Route("api/user-account")]
public class UserAccountApiController : ApiControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly UserManager<Users> _userManager;

    public UserAccountApiController(IUserAccountService userAccountService, UserManager<Users> userManager)
    {
        _userAccountService = userAccountService;
        _userManager = userManager;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Endpoint 1: Retorna os dados para o Card de Perfil.
    /// </summary>
    [HttpGet("card-info")]
    public async Task<IActionResult> GetCardInfo()
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        var isAdmin =
            (await _userManager.GetRolesAsync(user)).Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase));
        var userProfile = await _userAccountService.GetUserProfileAsync(userId);
        var subscription = await _userAccountService.GetUserSubscriptionDetailsAsync(userId);

        var cardData = new
        {
            IsAdmin = isAdmin,
            UserProfile = userProfile,
            Subscription = subscription
        };
        return Ok(cardData);
    }

    /// <summary>
    /// Endpoint 2: Retorna o histórico de pagamentos.
    /// </summary>
    [HttpGet("payment-history")]
    public async Task<IActionResult> GetPaymentHistory()
    {
        var history = await _userAccountService.GetUserPaymentHistoryAsync(GetUserId());
        return Ok(history);
    }

    /// <summary>
    /// Endpoint 3: Retorna os detalhes da assinatura para gerenciamento.
    /// </summary>
    [HttpGet("subscription-details")]
    public async Task<IActionResult> GetSubscriptionDetails()
    {
        var subscription = await _userAccountService.GetUserSubscriptionDetailsAsync(GetUserId());
        return Ok(subscription);
    }
}