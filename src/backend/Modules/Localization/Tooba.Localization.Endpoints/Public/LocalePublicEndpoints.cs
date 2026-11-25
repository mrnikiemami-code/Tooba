using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Localization.Application.Languages.Queries;

namespace Tooba.Localization.Endpoints.Public;

/// <summary>Read-only active language registry for non-admin surfaces; no mutation or admin data.</summary>
public static class LocalePublicEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/languages", ListActiveAsync);
    }

    private static async Task<IResult> ListActiveAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListActiveLanguagesQuery(), cancellationToken);
        if (result.IsFailure) return api.From(result);
        return Results.Json(result.Value);
    }
}
