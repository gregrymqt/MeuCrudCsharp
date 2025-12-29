using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MeuCrudCsharp.Models.Enums; // Certifique-se de usar o namespace onde criou os Enums acima

namespace MeuCrudCsharp.Models
{
    // Seu status interno (Mantido conforme )
    public enum InternalClaimStatus
    {
        [Display(Name = "Novo")]
        Novo,

        [Display(Name = "Em Análise")]
        EmAnalise,

        [Display(Name = "Respondido pelo Vendedor")]
        RespondidoPeloVendedor,

        [Display(Name = "Resolvido - Ganhamos")]
        ResolvidoGanhamos,

        [Display(Name = "Resolvido - Perdemos")]
        ResolvidoPerdemos
    }

    public class Claims
    {
        [Key]
        public int Id { get; set; } // ID Interno do Banco [cite: 19]

        // Este é o ID real que o MP usa (ex: 5012391221)
        [Required]
        public long MpClaimId { get; set; } // Mudei para long pois geralmente é numérico, mas string também funciona [cite: 20]

        // ID do pagamento vinculado (Resource ID)
        // CORREÇÃO: De 'ResorceId' para 'ResourceId'
        public string? ResourceId { get; set; }

        // Agora usando o Enum forte em vez de string
        [Required]
        [Column(TypeName = "nvarchar(50)")] // Salva como string no banco para legibilidade
        public ClaimType Type { get; set; } // "mediations", "cancel_purchase" etc [cite: 22]

        // Agora usando o Enum forte em vez de string
        [Column(TypeName = "nvarchar(50)")]
        public ClaimResource? ResourceType { get; set; } // "payment", "subscription" etc [cite: 23]

        // Novo campo sugerido para guardar o status original do MP (opened/closed) separadamente do seu status interno
        [Column(TypeName = "nvarchar(20)")]
        public ClaimStage? CurrentStage { get; set; } // claim, dispute, etc [cite: 4]

        public DateTime DataCreated { get; set; } = DateTime.UtcNow;

        // Seu status interno de controle
        public InternalClaimStatus Status { get; set; } = InternalClaimStatus.Novo; 

        public string? MercadoPagoPanelUrl =>
            $"https://www.mercadopago.com.br/developers/panel/notifications/claims/{MpClaimId}"; 

        [ForeignKey("user_id")]
        public string? UserId { get; set; }
    
        public virtual Users? User { get; set; }
    }
}