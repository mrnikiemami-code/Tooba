using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Fulfillment.Application.Commands.CreateShippingService;
using Tooba.Fulfillment.Application.Commands.DeactivateShippingService;
using Tooba.Fulfillment.Application.Commands.EnsureShippingCatalogSeed;
using Tooba.Fulfillment.Application.Commands.UpdateShippingService;
using Tooba.Fulfillment.Application.Queries.GetShippingService;
using Tooba.Fulfillment.Application.Queries.ListShippingServices;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Endpoints.Admin;

namespace Tooba.Fulfillment.Endpoints.Shipping;

/// <summary>Wire-only translation DTO.</summary>
public sealed record ShippingServiceTranslationWrite(Guid LanguageId, string Name, string? Description);
public sealed record ShippingServiceOptionTranslationWrite(Guid LanguageId, string Name);
public sealed record ShippingServiceOptionWrite(
    Guid? ShippingServiceOptionId, string Code, bool IsActive, int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationWrite> Translations);
public sealed record ShippingServiceWriteRequest(
    string Code, string ProviderKind, string IconKey, string ColorKey, bool IsActive, int SortOrder,
    IReadOnlyList<ShippingServiceTranslationWrite> Translations,
    IReadOnlyList<ShippingServiceOptionWrite> Options);

/// <summary>Admin shipping-services CRUD — module-owned.</summary>
public static class ShippingServiceEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        ArgumentNullException.ThrowIfNull(admin);
        var group = admin.MapGroup("/shipping-services");
        group.MapGet("/", ListAsync);
        group.MapGet("/{serviceId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{serviceId:guid}", UpdateAsync);
        group.MapPost("/{serviceId:guid}/deactivate", DeactivateAsync);
        group.MapPost("/ensure-seed", EnsureSeedHttpAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, string? language, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListShippingServicesQuery(language), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid serviceId, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetShippingServiceQuery(serviceId), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        ShippingServiceWriteRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new CreateShippingServiceCommand(ToModel(body)), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid serviceId, ShippingServiceWriteRequest body, ISender sender, IFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new UpdateShippingServiceCommand(serviceId, ToModel(body)), cancellationToken));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid serviceId, ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var result = await sender.Send(new DeactivateShippingServiceCommand(serviceId), cancellationToken);
        return result.IsFailure ? api.From(result) : Results.Json(new { ok = true });
    }

    private static async Task<IResult> EnsureSeedHttpAsync(
        ISender sender, IFulfillmentAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var result = await sender.Send(new EnsureShippingCatalogSeedCommand(), cancellationToken);
        return result.IsFailure ? api.From(result) : Results.Json(new { ok = true });
    }

    private static ShippingServiceWriteModel ToModel(ShippingServiceWriteRequest body) =>
        new(
            body.Code, body.ProviderKind, body.IconKey, body.ColorKey, body.IsActive, body.SortOrder,
            body.Translations.Select(t => new ShippingServiceTranslationWriteModel(t.LanguageId, t.Name, t.Description)).ToList(),
            body.Options.Select(o => new ShippingServiceOptionWriteModel(
                o.ShippingServiceOptionId, o.Code, o.IsActive, o.SortOrder,
                o.Translations.Select(t => new ShippingServiceOptionTranslationWriteModel(t.LanguageId, t.Name)).ToList())).ToList());
}
