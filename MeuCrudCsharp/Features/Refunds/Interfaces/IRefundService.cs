// Local: Features/Refunds/Interfaces/IRefundService.cs

namespace MeuCrudCsharp.Features.Refunds.Interfaces
{
    public interface IRefundService
    {
        /// <summary>
        /// Orquestra o processo de solicitação de reembolso para o usuário logado.
        /// </summary>
        Task RequestUserRefundAsync();
    }
}
