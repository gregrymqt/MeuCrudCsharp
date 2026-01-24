using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;

namespace MeuCrudCsharp.Features.Profiles.Admin.Services
{
    public class AdminStudentService : IAdminStudentService
    {
        private readonly IStudentRepository _repository; // <--- Mudou aqui
        private readonly ICacheService _cacheService;
        private readonly ILogger<AdminStudentService> _logger;

        public AdminStudentService(
            IStudentRepository repository, // <--- Injeção do Repository
            ICacheService cacheService,
            ILogger<AdminStudentService> logger
        )
        {
            _repository = repository;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<PaginatedResult<StudentDto>> GetAllStudentsAsync(int page, int pageSize)
        {
            var cacheKey = $"Admin_AllStudents_Page{page}_Size{pageSize}";

            return await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    try
                    {
                        _logger.LogInformation(
                            "Buscando a lista de alunos do banco de dados (cache miss)."
                        );

                        // Chamada ao Repositório
                        var (users, totalCount) = await _repository.GetAllWithSubscriptionsAsync(
                            page,
                            pageSize
                        );

                        // Se não tiver registros, retorna vazio
                        if (totalCount == 0)
                        {
                            return new PaginatedResult<StudentDto>
                            {
                                Items = [],
                                TotalCount = 0,
                                TotalPages = 0,
                                CurrentPage = page,
                            };
                        }

                        // Mapeamento (Entity -> DTO)
                        var studentDtos = users
                            .Select(u => new StudentDto(
                                u.PublicId.ToString(),
                                u.Name ?? "N/A",
                                u.Email ?? "N/A",
                                u.Subscription?.Status ?? "Sem Assinatura",
                                u.Subscription?.Plan?.Name ?? "N/A",
                                u.Subscription?.CreatedAt ?? DateTime.MinValue,
                                u.Subscription?.Id ?? "Sem Assinatura"
                            ))
                            .ToList();

                        // Monta o objeto de resultado
                        return new PaginatedResult<StudentDto>
                        {
                            Items = studentDtos,
                            TotalCount = totalCount,
                            CurrentPage = page,
                            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao buscar os alunos no repositório.");
                        throw new AppServiceException(
                            "An error occurred while querying student data.",
                            ex
                        );
                    }
                },
                absoluteExpireTime: TimeSpan.FromMinutes(5)
            ) ?? throw new InvalidOperationException();
        }

        public async Task<StudentDto> GetStudentByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Buscando o aluno pelo id no banco de dados (cache miss).");

                // Chamada ao Repositório
                var user = await _repository.GetByPublicIdWithSubscriptionAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("Tentativa de buscar aluno com ID {Guid} não encontrado.", id);
                    throw new KeyNotFoundException($"Aluno com ID {id} não encontrado.");
                }

                // Mapeamento (Entity -> DTO)
                var studentDto = new StudentDto(
                    user.PublicId.ToString(),
                    user.Name ?? "N/A",
                    user.Email ?? "N/A",
                    user.Subscription?.Status ?? "Sem Assinatura",
                    user.Subscription?.Plan?.Name ?? "N/A",
                    user.Subscription?.CreatedAt ?? DateTime.MinValue,
                    user.Subscription?.Id ?? "Sem Assinatura"
                );

                return studentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao buscar o aluno no repositório.");
                throw new AppServiceException("An error occurred while querying student data.", ex);
            }
        }
    }
}