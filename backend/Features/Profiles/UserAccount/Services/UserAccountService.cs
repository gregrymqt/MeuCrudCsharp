using MeuCrudCsharp.Features.Auth.Interfaces;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Files.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Clients.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Services;

public class UserAccountService : IUserAccountService
{
    private readonly IUserAccountRepository _repository;
    private readonly IFileService _fileService; // Injetamos o serviço de arquivos
    private readonly IUserContext _userContext; // Para pegar o ID do usuário logado
    private readonly ILogger<UserAccountService> _logger;

    public UserAccountService(
        IUserAccountRepository repository,
        IFileService fileService,
        IUserContext userContext,
        ILogger<UserAccountService> logger
    )
    {
        _repository = repository;
        _fileService = fileService;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<AvatarUpdateResponse> UpdateProfilePictureAsync(IFormFile file)
    {
        // 1. Identificar o usuário logado
        var userId = _userContext.GetCurrentUserId().ToString(); // Assumindo que sua interface expõe UserId string
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Usuário não identificado.");

        // 2. Buscar o usuário no banco
        var user = await _repository.GetUserByIdAsync(userId);
        if (user == null)
            throw new Exception("Usuário não encontrado.");

        // 3. Salvar o arquivo físico usando o FileService existente
        // Definimos uma categoria para organizar, ex: "avatars"
        var arquivoSalvo = await _fileService.SubstituirArquivoAsync(user.AvatarFileId, file);

        user.AvatarFileId = arquivoSalvo.Id; // ou arquivoSalvo.Url

        // 5. Persistir no banco de dados
        await _repository.SaveChangesAsync();

        _logger.LogInformation($"Avatar atualizado para o usuário {userId}");

        return new AvatarUpdateResponse
        {
            AvatarUrl = arquivoSalvo.CaminhoRelativo,
            Message = "Foto de perfil atualizada com sucesso!",
        };
    }
}
