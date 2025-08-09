using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    // Este DTO representa a estrutura da resposta de /preapproval_plan/search
    public class PlanSearchResponseDto
    {
        [JsonPropertyName("results")]
        public List<PlanResponseDto> Results { get; set; } = new List<PlanResponseDto>();
    }
}
