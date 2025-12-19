using System;
using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.ViewModels;

public class MercadoPagoClaimsViewModels
{

    // ==========================================
    // 1. AREA DE LISTAGEM (O "Inbox" do Admin)
    // ==========================================

    public class ClaimsIndexViewModel
    {
        public List<ClaimSummaryViewModel> Claims { get; set; } = new();

        // --- Filtros ---
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; } // ex: "opened", "closed"

        // --- Paginação ---
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ClaimSummaryViewModel
    {
        public int InternalId { get; set; } // ID do seu banco (1, 2, 3...)

        public long MpClaimId { get; set; } // ID do Mercado Pago (501239...) - CRUCIAL para o link

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public string ResourceId { get; set; } // ID do Pagamento/Curso vinculado

        public string Type { get; set; } // ex: "Produto com defeito", "Não reconhece compra"
        public string Status { get; set; } // "Aberto", "Em Mediação"

        public DateTime DateCreated { get; set; }

        // Ajuda o frontend a saber se pinta de vermelho (urgente) ou verde
        public bool IsUrgent { get; set; }
    }


    // ==========================================
    // 2. AREA DE DETALHES (A "Sala de Guerra")
    // ==========================================

    public class ClaimDetailViewModel
    {
        public int InternalId { get; set; }
        public long MpClaimId { get; set; }

        // Informações de Cabeçalho
        public string Status { get; set; }
        public string Stage { get; set; } // Em que pé está a briga
        public string PlayerRole { get; set; } // "respondent" (nós)

        // O Chat Completo
        public List<ClaimMessageViewModel> Messages { get; set; } = new();

        // Controle de UI
        public bool CanReply => Status != "closed"; // Bloqueia o input se já acabou
    }

    public class ClaimMessageViewModel
    {
        public string MessageId { get; set; }

        // Quem mandou?
        public string SenderRole { get; set; } // "complainant" (aluno), "respondent" (você), "mediator" (MP)

        // Texto
        public string Content { get; set; }

        public DateTime DateCreated { get; set; }

        public List<string> Attachments { get; set; } = new(); // URLs ou Nomes dos anexos

        // Propriedade auxiliar para o React saber se alinha à direita (nós) ou esquerda (eles)
        public bool IsMe { get; set; }
        public bool IsMediator => SenderRole == "mediator";
    }


    // ==========================================
    // 3. AREA DE AÇÃO (O Input de Resposta)
    // ==========================================

    public class ReplyClaimViewModel
    {
        [Required]
        public long InternalId { get; set; } // ID do MP para enviar

        [Required(ErrorMessage = "A mensagem não pode ser vazia.")]
        [MinLength(5, ErrorMessage = "Escreva uma resposta mais detalhada.")]
        public string Message { get; set; }

        // Opcional: Lista de nomes de arquivos se fizer upload
        public List<string>? Attachments { get; set; }
    }

}
