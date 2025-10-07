namespace MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;

public record PaymentReceiptDto(
    string PaymentId,
    DateTime CreatedAt,
    decimal Amount,
    string Status,
    string UserName,
    string CustomerCpf,
    string lastFourDigits
);