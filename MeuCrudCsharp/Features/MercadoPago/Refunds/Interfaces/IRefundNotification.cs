namespace MeuCrudCsharp.Features.MercadoPago.Refunds.Interfaces
{
    public interface IRefundNotification
    {
        Task SendRefundStatusUpdate(string userId, string status, string message);

    }
}
