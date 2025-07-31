using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class PlanResponseDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("date_created")]
        public DateTime DateCreated { get; set; }
    }
}
