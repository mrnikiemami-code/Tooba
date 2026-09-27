using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Variants.Models;

/// <summary>Body for PUT product variant-axes.</summary>
public sealed record SetProductVariantAxesWriteModel(List<Guid>? OrderedDefinitionIds);

/// <summary>Success payload for variant-axes PUT.</summary>
public sealed record ProductVariantAxesMutationOk(bool Ok = true);

/// <summary>Selected axis for preview/apply.</summary>
public sealed record ProductVariantSelectedAxisWriteModel(Guid DefinitionId, List<Guid>? OptionIds);

/// <summary>Body for POST variants/preview.</summary>
public sealed record ProductVariantPreviewWriteModel(
    string? Locale,
    List<ProductVariantSelectedAxisWriteModel>? SelectedAxes);

/// <summary>Patch row for apply (Status remains transport string).</summary>
public sealed record ProductVariantPatchWriteModel(
    Guid VariantId,
    string? Status,
    string? CatalogCodeSeam,
    int? SortOrder,
    bool? IsDefault);

/// <summary>Body for PUT variants/apply.</summary>
public sealed record ProductVariantApplyWriteModel(
    string? Locale,
    List<ProductVariantSelectedAxisWriteModel>? SelectedAxes,
    Guid? DefaultVariantId,
    List<ProductVariantPatchWriteModel>? VariantPatches);

/// <summary>Maps transport patch Status strings into domain status without throwing.</summary>
public static class ProductVariantPatchStatusMapper
{
    /// <summary>Parses optional status; returns false when non-blank and not a valid enum.</summary>
    public static bool TryParse(string? raw, out CatalogPublicationStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!Enum.TryParse<CatalogPublicationStatus>(raw.Trim(), true, out var parsed))
        {
            return false;
        }

        status = parsed;
        return true;
    }
}

/// <summary>Axis row for workspace single-variant create.</summary>
public sealed record WorkspaceVariantAxisWriteModel(Guid DefinitionId, string? RawValue, Guid? EnumOptionId);

/// <summary>Body for POST /v1/admin/products/{id}/variants (workspace).</summary>
public sealed record WorkspaceVariantCreateWriteModel(
    string? CatalogCodeSeam,
    IReadOnlyList<WorkspaceVariantAxisWriteModel>? Axes);

/// <summary>Body for PATCH /v1/admin/products/{id}/variants/{variantId} (workspace).</summary>
public sealed record WorkspaceVariantPatchWriteModel(string? Status, string? CatalogCodeSeam);
