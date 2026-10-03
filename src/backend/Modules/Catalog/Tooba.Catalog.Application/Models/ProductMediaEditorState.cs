using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>حالت ویرایشگر گالری رسانهٔ محصول.</summary>
public sealed record ProductMediaEditorState(
    Guid ProductId,
    IReadOnlyList<ProductMediaAssignment> Items,
    ProductMediaReadiness Readiness);
