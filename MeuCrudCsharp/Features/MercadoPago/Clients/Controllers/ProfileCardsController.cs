using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Clients.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.MercadoPago.Clients.Controllers;
/// <summary>
/// Endpoints para gerenciamento de cartões do perfil do usuário autenticado.
/// </summary>

[Route("api/profile/cards")]
public class ProfileCardsController : ApiControllerBase 
{
    private readonly IClientService _clientService;
    private readonly ILogger<ProfileCardsController> _logger;

    /// <summary>
    /// Cria uma nova instância do controlador de cartões do perfil.
    /// </summary>
    /// <param name="clientService">Serviço de clientes do provedor de pagamento.</param>
    /// <param name="logger">Logger para rastreamento e diagnóstico.</param>
    public ProfileCardsController(
        IClientService clientService,
        ILogger<ProfileCardsController> logger
    )
    {
        _clientService = clientService;
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
            var savedCardDto = await _clientService.AddCardToCustomerAsync(
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
            var cards = await _clientService.ListCardsFromCustomerAsync();
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
            var deletedCard = await _clientService.DeleteCardFromCustomerAsync(cardId);
            return Ok(deletedCard);
        }
        catch (AppServiceException ex)
        {
            _logger.LogError(ex, "Payment provider error while deleting card {CardId}.", cardId);
            return BadRequest(new { message = ex.Message });
        }
    }
    
}
