namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public record PaymentStatusUpdate(
        string Message,
        string Status,
        bool IsComplete,
        string? PaymentId = null,
        string? ExternalId = null
    );

    public interface IPaymentNotificationService
    {
        Task SendStatusUpdateAsync(string userId, PaymentStatusUpdate update);
    }
}
