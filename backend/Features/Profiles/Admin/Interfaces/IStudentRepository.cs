using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Profiles.Admin.Interfaces
{
    public interface IStudentRepository
    {
        // Retorna uma Tupla com a lista de usuários e o total de registros (para paginação)
        Task<(IEnumerable<Users> Items, int TotalCount)> GetAllWithSubscriptionsAsync(
            int page,
            int pageSize
        );

        // Busca um único usuário com os dados de assinatura carregados
        Task<Users?> GetByPublicIdWithSubscriptionAsync(Guid publicId);
    }
}
