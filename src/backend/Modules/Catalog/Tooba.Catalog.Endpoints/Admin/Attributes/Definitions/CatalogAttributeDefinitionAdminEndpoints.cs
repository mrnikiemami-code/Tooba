using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Attributes.Definitions.Commands;
using Tooba.Catalog.Application.Attributes.Definitions.Models;
using Tooba.Catalog.Application.Attributes.Definitions.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Attributes.Definitions;

/// <summary>Admin Catalog Attribute Definition HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogAttributeDefinitionAdminEndpoints
{
    /// <summary>Maps seven Admin Attribute Definition routes.</summary>
    public static void MapCatalogAttributeDefinitionAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var defs = app.MapGroup("/v1/admin/catalog/attribute-definitions");
        defs.MapGet("/", ListDefinitionsAsync);
        defs.MapGet("/{definitionId:guid}", GetDefinitionAsync);
        defs.MapPost("/", CreateDefinitionAsync);
        defs.MapPatch("/{definitionId:guid}", UpdateDefinitionAsync);
        defs.MapGet(
            "/{definitionId:guid}/variant-axis-capability/disable-preview",
            PreviewVariantAxisCapabilityDisableAsync);
        defs.MapPut("/{definitionId:guid}/variant-axis-capability", SetVariantAxisCapabilityAsync);
        defs.MapPost("/{definitionId:guid}/options", AddOptionAsync);
    }

    private static async Task<IResult> ListDefinitionsAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListAttributeDefinitionsQuery(), cancellationToken));
    }

    private static async Task<IResult> GetDefinitionAsync(
        Guid definitionId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetAttributeDefinitionQuery(definitionId), cancellationToken));
    }

    private static async Task<IResult> CreateDefinitionAsync(
        CreateAttributeDefinitionWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var result = await sender.Send(new CreateAttributeDefinitionCommand(body), cancellationToken);
        if (result.IsFailure)
        {
            return api.From(result);
        }

        return api.Created(
            $"/v1/admin/catalog/attribute-definitions/{result.Value.DefinitionId}",
            result);
    }

    private static async Task<IResult> UpdateDefinitionAsync(
        Guid definitionId,
        AttributeDefinitionMetadataWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpdateAttributeDefinitionCommand(definitionId, body),
            cancellationToken));
    }

    private static async Task<IResult> PreviewVariantAxisCapabilityDisableAsync(
        Guid definitionId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new PreviewVariantAxisCapabilityDisableQuery(definitionId),
            cancellationToken));
    }

    private static async Task<IResult> SetVariantAxisCapabilityAsync(
        Guid definitionId,
        SetVariantAxisCapabilityBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetVariantAxisCapabilityCommand(definitionId, body.IsVariantAxisAllowed),
            cancellationToken));
    }

    private static async Task<IResult> AddOptionAsync(
        Guid definitionId,
        AddAttributeOptionBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var result = await sender.Send(
            new AddAttributeOptionCommand(definitionId, body.Code, body.LocalizedNames),
            cancellationToken);
        if (result.IsFailure)
        {
            return api.From(result);
        }

        return api.Created(
            $"/v1/admin/catalog/attribute-definitions/{definitionId}/options/{result.Value.OptionId}",
            result);
    }
}

/// <summary>Body for PUT variant-axis-capability.</summary>
public sealed record SetVariantAxisCapabilityBody(bool IsVariantAxisAllowed);

/// <summary>Body for POST options.</summary>
public sealed record AddAttributeOptionBody(string Code, Dictionary<string, string>? LocalizedNames);
