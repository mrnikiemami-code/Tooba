using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Attributes.Definitions.Models;

/// <summary>HTTP/create body for Attribute Definition Admin.</summary>
public sealed record CreateAttributeDefinitionWriteModel(
    string Code,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxisAllowed,
    Dictionary<string, string>? LocalizedNames,
    AttributeDefinitionMetadataWriteModel? Metadata);
