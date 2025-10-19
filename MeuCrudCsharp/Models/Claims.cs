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
    public int Id { get; set; }

    /// <summary>
    /// O ID da notificação recebida (ex: o 'id' do webhook do Mercado Pago).
    /// </summary>
    [Required]
    public long NotificationId { get; set; }

    /// <summary>
    /// O ID do recurso associado (ex: o ID do reembolso ou da disputa).
    /// </summary>
    [Required]
    public string? ClaimId { get; set; }

    /// <summary>
    /// O tipo de notificação (ex: "refund", "chargeback", "dispute").
    /// </summary>
    [Required]
    public string? Type { get; set; }

    /// <summary>
    /// Data em que o registro foi criado no banco de dados.
    /// </summary>
    public DateTime DataCreated { get; set; } = DateTime.UtcNow;

    // --- NOVOS CAMPOS ADICIONADOS ---

    /// <summary>
    /// Status interno para acompanhamento da equipe.
    /// </summary>
    public ClaimStatus Status { get; set; } = ClaimStatus.Novo;

    /// <summary>
    /// Campo de texto para notas internas da equipe sobre a reclamação.
    /// </summary>
    public string? InternalNotes { get; set; }

    /// <summary>
    /// Link direto para a página de detalhes da claim no painel do Mercado Pago.
    /// </summary>
    public string? MercadoPagoClaimUrl { get; set; }

    /// <summary>
    /// Tipo de pagamento se foi por pagamento ou assinatura.
    /// </summary>
    public string? TypePayment { get; set; }

    /// <summary>
    /// Id referente ao user que realizou a claim.
    /// </summary>
    [ForeignKey("user_id")]
    public string? UserId { get; set; }

    public virtual Users? User { get; set; }
}
