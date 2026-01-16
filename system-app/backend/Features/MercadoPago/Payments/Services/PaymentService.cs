using System;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Payments.Dtos;
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Services;

public class PaymentService : MercadoPagoServiceBase, IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IHttpClientFactory httpClient,
        ILogger<PaymentService> logger
    )
        : base(httpClient, logger)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<List<PaymentHistoryDto>> GetUserPaymentHistoryAsync(string userId)
    {
        // 1. Usa o método exato do seu Repository
        var payments = await _paymentRepository.GetPaymentsByUserIdAndTypeAsync(userId);

        // 2. Mapeia a Model (Banco) para o DTO (Front)
        var historyDtos = payments
            .Select(p => new PaymentHistoryDto
            {
                // Usando ExternalId (do MP) como ID visual, ou p.PublicId.ToString() se preferir
                Id = p.ExternalId ?? p.Id.ToString(),
                Amount = p.Amount, // Assumindo que o nome da coluna no banco é TransactionAmount
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                PaymentMethod = p.Method, // ex: pix, credit_card

                // Lógica simples para descrição se estiver vazia
                Description = string.IsNullOrEmpty(p.Description)
                    ? $"Pagamento via {p.Method}"
                    : p.Description,
            })
            .ToList();

        return historyDtos;
    }
}
