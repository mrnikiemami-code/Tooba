namespace Tooba.Catalog.Application.Attributes.ProductValues.Models;

/// <summary>One bulk product-attribute value row.</summary>
public sealed record ProductAttributeValueWriteModel(
    Guid DefinitionId,
    string? RawValue,
    Guid? EnumOptionId,
    bool Clear);

/// <summary>Body for bulk product-attribute PUT.</summary>
public sealed record SetProductAttributesWriteModel(
    string? Locale,
    List<ProductAttributeValueWriteModel>? Values);

/// <summary>Body for single product-attribute PUT.</summary>
public sealed record SetProductAttributeWriteModel(string RawValue, Guid? EnumOptionId);

/// <summary>Success payload for single product-attribute PUT.</summary>
public sealed record ProductAttributeMutationOk(bool Ok = true);
