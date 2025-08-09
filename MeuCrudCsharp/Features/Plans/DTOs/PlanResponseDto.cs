using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    public class PlanResponseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("ExternalPlanId")]
        public string? ExternalPlanId { get; set; }
        public AutoRecurringDto? AutoRecurring { get; set; }
    }
}
