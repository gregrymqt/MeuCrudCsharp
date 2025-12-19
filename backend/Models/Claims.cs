using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models;

/// <summary>
/// Define o status interno de uma reclamação (claim) para acompanhamento.
/// </summary>
public enum ClaimStatus
{
    [Display(Name = "Novo")]
    Novo, // A reclamação acabou de ser registrada.

    [Display(Name = "Em Análise")]
    EmAnalise, // A equipe está investigando a reclamação.

    // ADICIONAR ESTE:
    [Display(Name = "Respondido pelo Vendedor")]
    RespondidoPeloVendedor,

    [Display(Name = "Resolvido - Ganhamos")]
    ResolvidoGanhamos, // A disputa foi resolvida a nosso favor.

    [Display(Name = "Resolvido - Perdemos")]
    ResolvidoPerdemos, // A disputa foi resolvida a favor do cliente.
}

/// <summary>
/// Representa uma notificação de claim (disputa, reembolso, etc.) recebida.
/// Ajuda a garantir a idempotência e o rastreamento de eventos.
/// </summary>
public class Claims
{
    [Key]
    public int Id { get; set; } // ID Interno do Banco

    // Este é o ID real que o MP usa (ex: 5012391221)
    [Required]
    public long MpClaimId { get; set; }

    // ID do pagamento vinculado (Resource ID)
    public string? PaymentId { get; set; }

    [Required]
    public string Type { get; set; } // mediations, payment, etc

    public DateTime DataCreated { get; set; } = DateTime.UtcNow;

    public ClaimStatus Status { get; set; } = ClaimStatus.Novo;

    // Link para o painel do MP (útil para o Admin clicar e ir direto)
    public string? MercadoPagoPanelUrl => $"https://www.mercadopago.com.br/developers/panel/notifications/claims/{MpClaimId}";

    [ForeignKey("user_id")]
    public string? UserId { get; set; }
    public virtual Users? User { get; set; }
}