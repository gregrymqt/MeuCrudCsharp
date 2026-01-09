// Usings combinados de todos os arquivos
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Files.Attributes;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Controllers;

[Route("api/user-account")] // Rota Base
public class UserAccountController : ApiControllerBase
{
    private readonly IUserAccountService _userAccountService;
    private readonly ILogger<UserAccountController> _logger;

    public UserAccountController(
        IUserAccountService userAccountService,
        ILogger<UserAccountController> logger
    )
    {
        _userAccountService = userAccountService;
        _logger = logger;
    }

    // Alinhado para POST
    [HttpPost("avatar")]
    [AllowLargeFile(20)] // Limite de 20MB está ótimo para avatar
    public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
    {
        // Validação defensiva
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Nenhum arquivo enviado." });
        }

        try
        {
            // O Service deve salvar direto (sem lógica de chunks aqui)
            var result = await _userAccountService.UpdateProfilePictureAsync(file);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar avatar.");
            return StatusCode(500, new { message = "Erro interno ao processar a imagem." });
        }
    }
}
