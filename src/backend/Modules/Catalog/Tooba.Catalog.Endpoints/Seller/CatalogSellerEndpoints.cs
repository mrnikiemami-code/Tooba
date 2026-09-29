using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Seller.Queries;
using Tooba.Catalog.Application.Variants.Commands;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Application.Attributes.ProductValues.Commands;
using Tooba.Catalog.Application.Attributes.ProductValues.Models;

namespace Tooba.Catalog.Endpoints.Seller;

/// <summary>
/// Seller Catalog HTTP — Catalog-owned via MediatR + ApiResponseFactory.
/// Three evacuated routes: published variant list, product attribute write, product variant-axes write.
/// </summary>
public static class CatalogSellerEndpoints
{
    /// <summary>Maps the three Seller Catalog routes.</summary>
    public static void MapCatalogSellerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/seller");
        group.MapGet("/catalog-variants", ListCatalogVariantsAsync);
        group.MapPut("/products/{productId:guid}/attributes/{definitionId:guid}", SetProductAttributeAsync);
        group.MapPut("/products/{productId:guid}/variant-axes", SetProductVariantAxesAsync);
    }

    private static async Task<IResult> ListCatalogVariantsAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogSellerAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListSellerCatalogVariantsQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SetProductAttributeAsync(
        Guid productId,
        Guid definitionId,
        SetProductAttributeRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogSellerAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetProductAttributeCommand(
                productId,
                definitionId,
                new SetProductAttributeWriteModel(body.RawValue, body.EnumOptionId)),
            cancellationToken));
    }

    private static async Task<IResult> SetProductVariantAxesAsync(
        Guid productId,
        SetProductVariantAxesRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogSellerAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        // رفتار قبلی Host: null → لیست خالی (پاک‌کردن محورها)؛ همان معنای بدنه حفظ می‌شود.
        var orderedDefinitionIds = body.OrderedDefinitionIds ?? [];
        return api.From(await sender.Send(
            new SetProductVariantAxesCommand(
                productId,
                new SetProductVariantAxesWriteModel(orderedDefinitionIds)),
            cancellationToken));
    }
}

/// <summary>
/// بدنهٔ مقدار ویژگی محصول — Seller panel HTTP transport (RawValue + EnumOptionId).
/// Relocated from Admin CatalogAttributeEndpoints in W10-R1 and evacuated from Host/Seller in R2;
/// not a shared business contract.
/// </summary>
public sealed record SetProductAttributeRequest(string RawValue, Guid? EnumOptionId);

/// <summary>
/// بدنهٔ محورهای Variant محصول — Seller panel HTTP transport.
/// Relocated from Admin CatalogAttributeEndpoints in W11 and evacuated from Host/Seller in R2;
/// not a shared business contract.
/// </summary>
public sealed record SetProductVariantAxesRequest(List<Guid>? OrderedDefinitionIds);
