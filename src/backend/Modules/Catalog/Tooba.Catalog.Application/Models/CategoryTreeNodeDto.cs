using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>گره درخت Admin Category برای Ant Tree آینده.</summary>
public sealed record CategoryTreeNodeDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    CatalogPublicationStatus Status,
    int SortOrder,
    bool IsVisible,
    bool HasChildren,
    int? ProductCount);
