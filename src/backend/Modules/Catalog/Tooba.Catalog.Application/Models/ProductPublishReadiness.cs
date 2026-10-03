using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// آمادگی تجمیعی انتشار Product Master — فقط Catalog.
/// Offer / Pricing / Inventory عمداً خارج‌اند.
/// </summary>
public sealed record ProductPublishReadiness(
    bool IsReady,
    bool CategoryReady,
    bool TranslationReady,
    bool AttributeReady,
    bool VariantReady,
    bool MediaReady,
    bool SeoReady,
    IReadOnlyList<ProductPublishMissingRequirement> MissingRequirements,
    string MessageFa);
