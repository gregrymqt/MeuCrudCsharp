using System.Threading.Tasks;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Jobs
{
    public interface INotificationPaymentService
    {
        // Recebe o ID do usuário, não o ClaimsPrincipal
        Task VerifyAndProcessNotificationAsync(Guid userId, string paymentID);
    }
}
