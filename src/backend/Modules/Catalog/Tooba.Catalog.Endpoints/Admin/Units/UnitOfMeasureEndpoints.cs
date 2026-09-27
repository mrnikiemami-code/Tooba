using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Units.Commands;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Units;

/// <summary>Admin UnitOfMeasure HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class UnitOfMeasureEndpoints
{
    /// <summary>Maps five routes under /v1/admin/catalog/units.</summary>
    public static void MapUnitOfMeasureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/catalog/units");
        group.MapGet("/", ListAsync);
        group.MapGet("/{unitId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{unitId:guid}", UpdateAsync);
        group.MapPost("/{unitId:guid}/deactivate", DeactivateAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        string? language,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListUnitOfMeasuresQuery(language), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid unitId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetUnitOfMeasureQuery(unitId), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        UnitOfMeasureWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new CreateUnitOfMeasureCommand(ToModel(body)), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid unitId,
        UnitOfMeasureWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new UpdateUnitOfMeasureCommand(unitId, ToModel(body)), cancellationToken));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid unitId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new DeactivateUnitOfMeasureCommand(unitId), cancellationToken));
    }

    private static UnitOfMeasureWriteModel ToModel(UnitOfMeasureWriteRequest body) =>
        new(
            body.Code,
            body.Dimension,
            body.IsActive,
            body.SortOrder,
            body.Translations
                .Select(t => new UnitOfMeasureTranslationWriteModel(t.LanguageId, t.Name, t.ShortName))
                .ToList());
}

/// <summary>Translation write transport DTO.</summary>
public sealed record UnitTranslationWrite(Guid LanguageId, string Name, string ShortName);

/// <summary>Create/update body for Admin units.</summary>
public sealed record UnitOfMeasureWriteRequest(
    string Code,
    string Dimension,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<UnitTranslationWrite> Translations);
