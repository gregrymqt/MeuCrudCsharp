namespace MeuCrudCsharp.Features.MercadoPago.Notification.Record;

public record PaymentStatusUpdate(
    string Message,
    string Status,
    bool IsComplete,
    string? PaymentId = null,
    string? ExternalId = null
);