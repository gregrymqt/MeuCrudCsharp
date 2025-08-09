using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Features.Plans.DTOs
{
    /// <summary>
    /// Represents the paginated response structure from the payment provider's plan search endpoint (e.g., /preapproval_plan/search).
    /// </summary>
    public class PlanSearchResponseDto
    {
        /// <summary>
        /// A list containing the plan details that match the search criteria.
        /// </summary>
        [JsonPropertyName("results")]
        public List<PlanResponseDto> Results { get; set; } = new List<PlanResponseDto>();
    }
}
