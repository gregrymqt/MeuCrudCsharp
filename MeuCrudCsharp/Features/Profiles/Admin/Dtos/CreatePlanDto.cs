using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class CreatePlanDto
    {
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("auto_recurring")]
        public AutoRecurringDto? AutoRecurring { get; set; }

        [JsonPropertyName("back_url")]
        public string? BackUrl { get; set; }
    }
}
