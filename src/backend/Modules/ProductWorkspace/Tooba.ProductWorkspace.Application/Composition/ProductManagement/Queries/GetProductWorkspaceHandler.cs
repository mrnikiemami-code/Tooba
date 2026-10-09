using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition.ProductManagement.Models;
using Tooba.Tax.Contracts.Ports;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Queries;

/// <summary>
/// Composes Admin ProductWorkspace aggregate GET through Contracts-only foreign ports.
/// </summary>
public sealed class GetProductWorkspaceHandler(
    ICatalogAdminProductWorkspaceReadGateway catalog,
    IOfferQueryGateway offers,
    IPriceQueryGateway prices,
    IInventoryQueryGateway inventory,
    ITaxQueryGateway tax,
    IPartyLookup parties) : IRequestHandler<GetProductWorkspaceQuery, Result<ProductWorkspaceView>>
{
    private static readonly IReadOnlyList<string> UnsupportedMutations =
    [
        "media-binary-upload",
        "product-video-upload",
        "promotion-write",
        "full-content-studio"
    ];

    /// <inheritdoc />
    public async Task<Result<ProductWorkspaceView>> Handle(
        GetProductWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await catalog.GetAggregateSnapshotAsync(request.ProductId, cancellationToken);
        if (snapshot is null)
        {
            return Result.Failure<ProductWorkspaceView>(
                new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        }

        var variantIds = snapshot.Variants.Select(v => v.VariantId).ToList();
        var offerRows = variantIds.Count == 0
            ? []
            : (await offers.ListOffersByCatalogVariantIdsAsync(variantIds, cancellationToken)).ToList();
        var offerIds = offerRows.Select(x => x.OfferId).ToList();
        var priceRows = offerIds.Count == 0
            ? []
            : await prices.ListByOfferIdsAsync(offerIds, cancellationToken);
        var positions = offerIds.Count == 0
            ? []
            : await inventory.ListPositionsByOfferIdsAsync(offerIds, cancellationToken);
        var locationIds = positions.Select(x => x.LocationId).Distinct().ToList();
        var locations = locationIds.Count == 0
            ? []
            : await inventory.ListLocationsByIdsAsync(locationIds, cancellationToken);
        var taxRows = offerIds.Count == 0
            ? []
            : await tax.ListClassificationsByOfferIdsAsync(offerIds, cancellationToken);
        var taxCats = taxRows.Count == 0
            ? []
            : await tax.ListCategoriesByIdsAsync(taxRows.Select(r => r.CategoryId).Distinct().ToArray(), cancellationToken);

        var sellerIds = offerRows.Select(o => o.SellerPartyId).Distinct().ToList();
        var sellerNames = sellerIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await parties.GetDisplayNamesAsync(sellerIds, cancellationToken);

        var offerViews = offerRows.Select(offer =>
        {
            sellerNames.TryGetValue(offer.SellerPartyId, out var displayName);
            return new ProductOfferView(
                offer.OfferId,
                offer.CatalogVariantId,
                offer.SellerPartyId,
                string.IsNullOrWhiteSpace(displayName) ? "فروشنده" : displayName!,
                offer.Status.ToString(),
                offer.Channel.ToString(),
                offer.SellerSku);
        }).ToList();

        var priceViews = priceRows.Select(p => new ProductPriceView(
            p.PriceId,
            p.OfferId,
            p.Market,
            p.Currency,
            p.Amount,
            p.Status,
            p.ValidFrom,
            p.ValidTo)).ToList();

        var taxViews = taxRows.Select(row =>
        {
            var cat = taxCats.Single(c => c.CategoryId == row.CategoryId);
            return new ProductTaxView(row.OfferId, cat.CategoryId, cat.Code, cat.DisplayName);
        }).ToList();

        var stockViews = positions.Select(pos =>
        {
            var loc = locations.Single(l => l.LocationId == pos.LocationId);
            return new ProductStockView(pos.OfferId, loc.LocationId, loc.Code, loc.Name, pos.OnHand, pos.Reserved, pos.Available);
        }).ToList();

        var variantViews = snapshot.Variants.Select(v => new ProductVariantView(
            v.VariantId,
            v.Fingerprint,
            v.Status,
            v.CatalogCodeSeam,
            offerRows.Count(o => o.CatalogVariantId == v.VariantId),
            stockViews.Where(s => offerRows.Any(o => o.OfferId == s.OfferId && o.CatalogVariantId == v.VariantId))
                .Select(s => s.LocationId)
                .Distinct()
                .Count())).ToList();

        var commercialWarnings = new List<string>();
        if (offerRows.All(o => o.Status != OfferStatus.Active))
        {
            commercialWarnings.Add("پیشنهاد فروشندهٔ فعالی ثبت نشده است");
        }

        if (priceViews.Count == 0)
        {
            commercialWarnings.Add("قیمت فروشنده ثبت نشده است");
        }

        if (stockViews.All(s => s.Available <= 0))
        {
            commercialWarnings.Add("موجودی قابل‌فروش وجود ندارد");
        }

        var purchasable = offerRows.Any(o => o.Status == OfferStatus.Active)
            && priceViews.Count > 0
            && stockViews.Any(s => s.Available > 0);

        var catalogChecks = snapshot.CatalogReadinessMessages.ToList();
        var warnings = new List<string>(catalogChecks);
        warnings.AddRange(commercialWarnings);
        if (!string.IsNullOrWhiteSpace(snapshot.PrimaryCategoryAssignableWarningFa))
        {
            warnings.Add(snapshot.PrimaryCategoryAssignableWarningFa);
        }

        var publication = new ProductPublicationView(
            snapshot.Status,
            purchasable,
            catalogChecks,
            new ProductPublishReadinessView(
                snapshot.PublishReadiness.IsReady,
                snapshot.PublishReadiness.CategoryReady,
                snapshot.PublishReadiness.TranslationReady,
                snapshot.PublishReadiness.AttributeReady,
                snapshot.PublishReadiness.VariantReady,
                snapshot.PublishReadiness.MediaReady,
                snapshot.PublishReadiness.SeoReady,
                snapshot.PublishReadiness.MissingRequirements
                    .Select(m => new ProductPublishMissingRequirementView(m.Code, m.MessageFa, m.WorkspaceTab))
                    .ToList(),
                snapshot.PublishReadiness.MessageFa),
            snapshot.UpdatedAt);

        return Result.Success(new ProductWorkspaceView(
            snapshot.ProductId,
            snapshot.Title,
            snapshot.Status,
            snapshot.Kind,
            snapshot.BrandName,
            snapshot.CategoryNames,
            snapshot.Attributes.Select(a => new ProductAttributeView(a.Code, a.Value, a.VariantAxis)).ToList(),
            variantViews,
            snapshot.Media.Select(m => new ProductMediaView(m.MediaAssetId, m.Primary, m.DisplayOrder, m.AltText)).ToList(),
            offerViews,
            priceViews,
            taxViews,
            stockViews,
            new ProductSeoView(snapshot.SlugSeam, snapshot.SeoTitleSeam, ""),
            publication,
            snapshot.Activity.Select(MapHistory).ToList(),
            snapshot.Audit.Select(MapHistory).ToList(),
            request.Permissions,
            snapshot.UpdatedAt,
            warnings,
            UnsupportedMutations,
            snapshot.PrimaryCategoryId,
            snapshot.CategoryPath,
            snapshot.SlugSeam,
            snapshot.ShortDescription,
            snapshot.Translations.Select(t => new ProductTranslationView(
                t.Locale, t.Name, t.Slug, t.ShortDescription, t.Description, t.SeoTitle, t.SeoDescription)).ToList(),
            snapshot.IsPrimaryCategoryAssignable,
            snapshot.BrandId,
            snapshot.CategoryAssignments.Select(c => new ProductCategoryAssignmentView(c.CategoryId, c.CategoryPath, c.Role)).ToList(),
            snapshot.UnitOfMeasureId,
            snapshot.QuantityDecimalPlaces,
            snapshot.QuantityStep,
            snapshot.UnitCode,
            snapshot.UnitDisplayName,
            snapshot.Units.Select(u => new UnitOfMeasureOptionView(u.UnitOfMeasureId, u.Code, u.Name, u.ShortName)).ToList()));
    }

    private static ProductHistoryItem MapHistory(CatalogAdminHistoryItem x) =>
        new(x.Kind, x.Summary, x.At, x.Actor, x.Section, x.BeforeSummary, x.AfterSummary, x.HistoryId);
}

