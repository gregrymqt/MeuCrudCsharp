using System.Security.Claims;
using MeuCrudCsharp.Data; // Supondo que o ApiDbContext esteja aqui
using MeuCrudCsharp.Features.Clients.DTOs; // Supondo que o CardRequestDto esteja aqui
using MeuCrudCsharp.Features.Clients.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Necessário para o FirstOrDefaultAsync

[ApiController]
[Route("api/profile/cards")]
[Authorize]
public class ProfileCardsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ApiDbContext _apiDbContext;
    private readonly ILogger<ProfileCardsController> _logger;

    public ProfileCardsController(
        IClientService clientService,
        ApiDbContext apiDbContext,
        ILogger<ProfileCardsController> logger
    )
    {
        _clientService = clientService;
        _apiDbContext = apiDbContext;
        _logger = logger;
    }

    // --- CREATE ---
    // POST api/profile/cards
    [HttpPost]
    public async Task<IActionResult> AddNewCard([FromBody] CardRequestDto request)
    {
        try
        {
            var customerId = await GetCurrentUserCustomerIdAsync();
            var savedCardDto = await _clientService.AddCardToCustomerAsync(
                customerId,
                request.Token
            );
            return Ok(savedCardDto);
        }
        catch (AppServiceException ex)
        {
            _logger.LogError(ex, "Erro no provedor de pagamento ao adicionar cartão.");
            return BadRequest(new { message = ex.Message });
        }
    }

    // --- READ ---
    // GET api/profile/cards
    [HttpGet]
    public async Task<IActionResult> ListMyCards()
    {
        try
        {
            var customerId = await GetCurrentUserCustomerIdAsync();
            var cards = await _clientService.ListCardsFromCustomerAsync(customerId);
            return Ok(cards);
        }
        catch (AppServiceException ex)
        {
            _logger.LogError(ex, "Erro no provedor de pagamento ao listar cartões.");
            return BadRequest(new { message = ex.Message });
        }
    }

    // --- DELETE ---
    // DELETE api/profile/cards/{cardId}
    [HttpDelete("{cardId}")]
    public async Task<IActionResult> DeleteMyCard(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return BadRequest(new { message = "O ID do cartão é obrigatório." });
        }

        try
        {
            var customerId = await GetCurrentUserCustomerIdAsync();

            // Lógica de negócio: não permitir deletar o cartão usado na assinatura
            // (Você pode adicionar essa verificação aqui antes de chamar o serviço)

            var deletedCard = await _clientService.DeleteCardFromCustomerAsync(customerId, cardId);
            return Ok(deletedCard);
        }
        catch (AppServiceException ex)
        {
            _logger.LogError(
                ex,
                "Erro no provedor de pagamento ao deletar o cartão {CardId}.",
                cardId
            );
            return BadRequest(new { message = ex.Message });
        }
    }

    // =======================================================
    // MÉTODO AUXILIAR PRIVADO
    // =======================================================
    private async Task<string> GetCurrentUserCustomerIdAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new AppServiceException("Não foi possível identificar o usuário na sessão.");
        }

        var user = await _apiDbContext
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userIdString);

        if (user == null)
        {
            throw new AppServiceException("Usuário não encontrado.");
        }

        if (string.IsNullOrEmpty(user.MercadoPagoCustomerId))
        {
            throw new AppServiceException("Usuário não possui um cliente de pagamentos associado.");
        }

        return user.MercadoPagoCustomerId;
    }
}
