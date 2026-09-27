namespace Tooba.Catalog.Application.ProductPublishing.Models;

/// <summary>Admin HTTP publish-readiness — preserves Host JSON property names.</summary>
public sealed record ProductPublishReadinessView(
    bool IsReady,
    bool CategoryReady,
    bool TranslationReady,
    bool AttributeReady,
    bool VariantReady,
    bool MediaReady,
    bool SeoReady,
    IReadOnlyList<ProductPublishMissingRequirementView> MissingRequirements,
    string MessageFa);

/// <summary>Admin HTTP missing requirement row — preserves Host JSON property names.</summary>
public sealed record ProductPublishMissingRequirementView(
    string Code,
    string MessageFa,
    string WorkspaceTab);
