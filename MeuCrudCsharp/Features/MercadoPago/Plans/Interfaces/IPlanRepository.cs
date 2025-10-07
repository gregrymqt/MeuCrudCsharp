using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;

public interface IPlanRepository
{
    // Métodos de escrita (já definidos anteriormente)
    Task AddAsync(Plan plan);
    void Update(Plan plan);
    Task<int> SaveChangesAsync();
    void Remove(object payload);

    // --- Métodos de Leitura ---

    /// <summary>
    /// Busca um plano pelo seu ID público. Lança ResourceNotFoundException se não encontrar.
    /// </summary>
    Task<Plan?> GetByPublicIdAsync(Guid publicId, bool asNoTracking);

    /// <summary>
    /// Busca um plano ativo pelo seu ID externo (do provedor de pagamento).
    /// </summary>
    Task<Plan?> GetActiveByExternalIdAsync(string externalId);

    /// <summary>
    /// Busca uma lista de planos pelos seus IDs externos.
    /// </summary>
    Task<List<Plan>> GetByExternalIdsAsync(IEnumerable<string> externalIds);

    /// <summary>
    /// Busca todos os planos que estão marcados como ativos no banco de dados.
    /// </summary>
    /// <returns>Uma lista de entidades Plan ativas.</returns>
    Task<List<Plan>> GetActivePlansAsync(); // <-- NOVO MÉTODO
}