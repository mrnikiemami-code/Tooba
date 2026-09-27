namespace Tooba.Catalog.Application.ProductMedia.Models;

/// <summary>Admin HTTP body for attaching an existing media asset reference.</summary>
public sealed record AttachProductMediaWriteModel(Guid MediaAssetId, string? AltText);

/// <summary>Admin HTTP body for generated placeholder media (optional alt).</summary>
public sealed record AttachPlaceholderProductMediaWriteModel(string? AltText);

/// <summary>Admin HTTP body for exact-set media reorder.</summary>
public sealed record ReorderProductMediaWriteModel(IReadOnlyList<Guid> OrderedMediaAssetIds);

/// <summary>Admin HTTP body for patching media alt text.</summary>
public sealed record PatchProductMediaWriteModel(string? AltText);

/// <summary>
/// Product media list item for Admin HTTP — property name <c>Primary</c> preserves Host JSON parity.
/// </summary>
public sealed record ProductMediaItemView(
    Guid MediaAssetId,
    bool Primary,
    int DisplayOrder,
    string? AltText);

/// <summary>Gallery readiness view for Admin HTTP.</summary>
public sealed record ProductMediaReadinessView(
    bool HasPrimaryImage,
    int MediaCount,
    bool IsReady,
    string? MessageFa);
