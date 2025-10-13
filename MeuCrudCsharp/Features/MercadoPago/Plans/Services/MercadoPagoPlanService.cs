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

    public async Task<IEnumerable<PlanResponseDto>> SearchActivePlansAsync()
    {
        const string endpoint = "/preapproval_plan/search";
        var responseBody = await SendMercadoPagoRequestAsync(HttpMethod.Get, endpoint, (object?)null);
        var apiResponse = JsonSerializer.Deserialize<PlanSearchResponseDto>(responseBody);

        return apiResponse?.Results?.Where(plan => plan.Status == "active" && plan.AutoRecurring != null)
               ?? Enumerable.Empty<PlanResponseDto>();
    }
}