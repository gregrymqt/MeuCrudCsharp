using System.Text;
using System.Text.Json;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Caching.Interfaces;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.MercadoPago.Base;
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Interfaces;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Services;

public class MercadoPagoPlanService : MercadoPagoServiceBase, IMercadoPagoPlanService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlanService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making API requests, passed to the base class.</param>
    /// <param name="logger">The logger for recording events and errors, passed to the base class.</param>
    public MercadoPagoPlanService(
        IHttpClientFactory httpClient,
        ILogger<IMercadoPagoPlanService> logger
    )
        : base(httpClient, logger)
    {
    }

    public async Task<PlanResponseDto> CreatePlanAsync(object payload)
    {
        const string endpoint = "/preapproval_plan";
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Post, endpoint, payload);
        return JsonSerializer.Deserialize<PlanResponseDto>(responseBody);
    }

    public async Task<PlanResponseDto> UpdatePlanAsync(string externalPlanId, object payload)
    {
        var endpoint = $"/preapproval_plan/{externalPlanId}";
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
        return JsonSerializer.Deserialize<PlanResponseDto>(responseBody);
    }

    public async Task CancelPlanAsync(string externalPlanId)
    {
        var endpoint = $"/preapproval_plan/{externalPlanId}";
        var payload = new { status = "cancelled" };
        await SendMercadoPagoRequestAsync(HttpMethod.Put, endpoint, payload);
    }

    public async Task<IEnumerable<PlanResponseDto>> SearchActivePlansAsync(int limit, int offset, string status,
        string sortBy, string criteria)
    {
        // Usamos um StringBuilder para construir a query string dinamicamente
        var queryString = new StringBuilder();
        queryString.Append($"/preapproval_plan/search?status={status}");
        queryString.Append($"&limit={limit}");
        queryString.Append($"&offset={offset}");
        queryString.Append($"&sort={sortBy}");
        queryString.Append($"&criteria={criteria}");

        var endpoint = queryString.ToString();

        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Get, endpoint, (object?)null);
        var apiResponse = JsonSerializer.Deserialize<PlanSearchResponseDto>(responseBody);

        // A filtragem de status agora é feita pela API, então o .Where() foi removido.
        return apiResponse?.Results?.Where(plan => plan.AutoRecurring != null)
               ?? Enumerable.Empty<PlanResponseDto>();
    }
}