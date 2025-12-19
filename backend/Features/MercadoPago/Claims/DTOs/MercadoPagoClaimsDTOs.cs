using System;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.MercadoPago.Claims.DTOs;

public class MercadoPagoClaimsDTOs
{
    public class MpClaimSearchResponse
    {
        [JsonPropertyName("paging")]
        public MpPaging Paging { get; set; }

        [JsonPropertyName("results")]
        public List<MpClaimItem> Results { get; set; }
    }

    public class MpPaging
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }

    public class MpClaimItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; } // ID da Reclamação no MP

        [JsonPropertyName("resource_id")]
        public string ResourceId { get; set; } // ID do Pagamento vinculado

        [JsonPropertyName("status")]
        public string Status { get; set; } // opened, closed, refund_delivered

        [JsonPropertyName("type")]
        public string Type { get; set; } // mediations, returns, etc.

        [JsonPropertyName("stage")]
        public string Stage { get; set; } // dispute, claim, etc.

        [JsonPropertyName("players")]
        public List<MpPlayer> Players { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTime LastUpdated { get; set; }
    }

    public class MpPlayer
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } // "complainant" (comprador), "respondent" (você)

        [JsonPropertyName("id")]
        public long UserId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } // "user"
    }

    // ==========================================
    // MENSAGENS (CHAT)
    // ==========================================
    public class MpMessageResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("sender_role")]
        public string SenderRole { get; set; } // "complainant", "respondent", "mediator"

        [JsonPropertyName("receiver_role")]
        public string ReceiverRole { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("attachments")]
        public List<MpAttachment>? Attachments { get; set; }
    }

    public class MpAttachment
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("original_filename")]
        public string OriginalFilename { get; set; }
    }

    // ==========================================
    // PAYLOADS DE ENVIO (REQUESTS)
    // ==========================================
    public class MpPostMessageRequest
    {
        [JsonPropertyName("receiver_role")]
        public string ReceiverRole { get; set; } // "complainant" ou "respondent" 

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("attachments")]
        public List<string>? Attachments { get; set; } // Lista de nomes de arquivos 
    }

    public class ReplyRequestDto
    {
        public string Message { get; set; }
    }

}
