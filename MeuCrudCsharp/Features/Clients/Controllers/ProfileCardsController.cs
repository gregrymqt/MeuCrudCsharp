using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Clients.DTOs;
using MeuCrudCsharp.Features.Clients.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Endpoints para gerenciamento de cartões do perfil do usuário autenticado.
/// </summary>
[ApiController]
[Route("api/profile/cards")]
[Authorize]
public class ProfileCardsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ApiDbContext _apiDbContext;
    private readonly ILogger<ProfileCardsController> _logger;

    /// <summary>
    /// Cria uma nova instância do controlador de cartões do perfil.
    /// </summary>
    /// <param name="clientService">Serviço de clientes do provedor de pagamento.</param>
    /// <param name="apiDbContext">Contexto de dados da aplicação.</param>
    /// <param name="logger">Logger para rastreamento e diagnóstico.</param>
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

    /// <summary>
    /// Adiciona um novo cartão ao cliente do usuário atual.
    /// </summary>
    /// <param name="request">Dados do cartão contendo o token do provedor.</param>
    /// <returns>Dados do cartão salvo.</returns>
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

    /// <summary>
    /// Lista os cartões vinculados ao cliente do usuário atual.
    /// </summary>
    /// <returns>Lista de cartões do cliente.</returns>
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

    /// <summary>
    /// Remove um cartão específico do cliente do usuário atual.
    /// </summary>
    /// <param name="cardId">Identificador do cartão no provedor.</param>
    /// <returns>Confirmação da remoção.</returns>
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
            var deletedCard = await _clientService.DeleteCardFromCustomerAsync(customerId, cardId);
            return Ok(deletedCard);
        }
        catch (AppServiceException ex)
        {
            _logger.LogError(ex, "Payment provider error while deleting card {CardId}.", cardId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the payment provider's customer identifier for the current user.
    /// </summary>
    /// <remarks>
    /// Throws an <see cref="AppServiceException"/> if the user is not identified, does not exist, or has no associated customer profile.
    /// Obtém o identificador de cliente no provedor de pagamentos para o usuário atual.
    /// <returns>The payment provider's customer identifier.</returns>
    /// <remarks>Lança <see cref="AppServiceException"/> quando usuário não está identificado, não existe ou não possui cliente associado.</remarks>
    /// <returns>Identificador do cliente no provedor de pagamentos.</returns>
    public async Task<string> GetCurrentUserCustomerIdAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            throw new AppServiceException("Não foi possível identificar o usuário na sessão.");
        }

        var users = _apiDbContext.Users;

        // 2. Find the user in the database.
        var user = await users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userIdString);

        if (user == null)
            throw new AppServiceException("User not found.");

        if (string.IsNullOrEmpty(user.MercadoPagoCustomerId))
        {
            _logger.LogWarning(
                "User {UserId} does not have an associated payment customer profile.",
                user.Id
            );
            throw new AppServiceException("Usuário não possui um cliente de pagamentos associado.");
        }

        return user.MercadoPagoCustomerId;
    }
}
