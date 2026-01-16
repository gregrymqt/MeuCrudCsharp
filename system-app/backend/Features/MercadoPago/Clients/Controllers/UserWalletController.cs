using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Clients.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Controllers;

[Route("api/v1/wallet")]
public class UserWalletController : MercadoPagoApiControllerBase
{
    private readonly ClientService _clientService;
    private readonly IUserContext _userContext;

    public UserWalletController(ClientService clientService, IUserContext userContext)
    {
        _clientService = clientService;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserCards()
    {
        try
        {
            var userId = await _userContext.GetCurrentUserId();
            var cards = await _clientService.GetUserWalletAsync(userId);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Erro ao listar cartões");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddCard([FromBody] AddCardRequestDto request)
    {
        try
        {
            var userId = await _userContext.GetCurrentUserId();
            var newCard = await _clientService.AddCardToWalletAsync(userId, request.CardToken);
            return Created("", newCard);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Erro ao adicionar cartão");
        }
    }

    [HttpDelete("{cardId}")]
    public async Task<IActionResult> DeleteCard(string cardId)
    {
        try
        {
            var userId = await _userContext.GetCurrentUserId();
            await _clientService.RemoveCardFromWalletAsync(userId, cardId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Erro ao remover cartão");
        }
    }
}
