namespace Tooba.Catalog.Application.Attributes.Definitions.Models;

/// <summary>Metadata fields for create follow-up or PATCH update.</summary>
public sealed record AttributeDefinitionMetadataWriteModel(
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    decimal? ValidationMin,
    decimal? ValidationMax,
    int? ValidationMaxLength,
    bool IsActive);
