
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;

public interface IPlanService
{
    /// <summary>
    /// Busca todos os planos ativos do banco de dados, formatados para exibição.
    /// </summary>
    /// <returns>Uma lista de DTOs dos planos ativos.</returns>
    Task<PagedResultDto<PlanDto>> GetActiveDbPlansAsync(int page, int pageSize);

    /// <summary>
    /// Busca um plano específico pelo seu identificador público (GUID).
    /// </summary>
    /// <param name="publicId">O ID público do plano a ser buscado.</param>
    /// <returns>O DTO do plano encontrado ou null se não existir.</returns>
    Task<PlanEditDto> GetPlanEditDtoByIdAsync(Guid publicId);

    /// <summary>
    /// Cria um novo plano de assinatura no sistema e no provedor de pagamento.
    /// </summary>
    /// <param name="createDto">O objeto de transferência de dados com as informações para a criação do plano.</param>
    /// <returns>A entidade do plano que foi criada.</returns>
    Task<PlanDto> CreatePlanAsync(CreatePlanDto createDto);

    /// <summary>
    /// Atualiza as informações de um plano de assinatura existente.
    /// </summary>
    /// <param name="publicId">O ID público do plano a ser atualizado.</param>
    /// <param name="updateDto">O objeto de transferência de dados com as informações a serem atualizadas.</param>
    /// <returns>A entidade do plano após a atualização.</returns>
    Task<PlanDto> UpdatePlanAsync(Guid publicId, UpdatePlanDto updateDto);

    /// <summary>
    /// Desativa um plano de assinatura (soft delete), tornando-o inativo.
    /// </summary>
    /// <param name="publicId">O ID público do plano a ser desativado.</param>
    /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
    Task DeletePlanAsync(Guid publicId);


    /// <summary>
    /// retorna todos os planos, buscando na api do Mercado Pago 
    /// </summary>
    /// <returns>Todos os planos</returns>
    Task<PagedResultDto<PlanDto>> GetActiveApiPlansAsync(int page, int pageSize);

}