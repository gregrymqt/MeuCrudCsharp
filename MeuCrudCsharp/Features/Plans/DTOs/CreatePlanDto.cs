using System.Text.Json.Serialization;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    public class CreatePlanDto
    {
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("auto_recurring")]
        public AutoRecurringDto? AutoRecurring { get; set; }

        [JsonPropertyName("back_url")]
        public string? BackUrl { get; set; }

        public string? ExternalPlanId { get; set; }
    }
}
