using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Attributes.Schema.Commands;
using Tooba.Catalog.Application.Attributes.Schema.Models;
using Tooba.Catalog.Application.Attributes.Schema.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Attributes.Schema;

/// <summary>Admin Catalog Category Attribute-Schema HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogCategoryAttributeSchemaAdminEndpoints
{
    /// <summary>Maps five Admin Category Attribute-Schema routes.</summary>
    public static void MapCatalogCategoryAttributeSchemaAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/attribute-schema");
        categories.MapGet("/effective", GetEffectiveSchemaAsync);
        categories.MapPost("/bindings", BindAsync);
        categories.MapPatch("/bindings/{definitionId:guid}", UpdateBindingAsync);
        categories.MapDelete("/bindings/{definitionId:guid}", UnbindAsync);
        categories.MapPut("/bindings/order", ReorderBindingsAsync);
    }

    private static async Task<IResult> GetEffectiveSchemaAsync(
        Guid categoryId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetEffectiveCategorySchemaQuery(categoryId), cancellationToken));
    }

    private static async Task<IResult> BindAsync(
        Guid categoryId,
        BindCategoryAttributeWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var result = await sender.Send(new BindCategoryAttributeCommand(categoryId, body), cancellationToken);
        if (result.IsFailure)
        {
            return api.From(result);
        }

        return api.Created(
            $"/v1/admin/catalog/categories/{categoryId}/attribute-schema/bindings/{body.DefinitionId}",
            result);
    }

    private static async Task<IResult> UpdateBindingAsync(
        Guid categoryId,
        Guid definitionId,
        UpdateCategoryAttributeBindingWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpdateCategoryAttributeBindingCommand(categoryId, definitionId, body),
            cancellationToken));
    }

    private static async Task<IResult> UnbindAsync(
        Guid categoryId,
        Guid definitionId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UnbindCategoryAttributeCommand(categoryId, definitionId),
            cancellationToken));
    }

    private static async Task<IResult> ReorderBindingsAsync(
        Guid categoryId,
        ReorderCategoryBindingsBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new ReorderCategoryAttributeBindingsCommand(categoryId, body.OrderedDefinitionIds),
            cancellationToken));
    }
}

/// <summary>Body for PUT bindings/order.</summary>
public sealed record ReorderCategoryBindingsBody(List<Guid>? OrderedDefinitionIds);
