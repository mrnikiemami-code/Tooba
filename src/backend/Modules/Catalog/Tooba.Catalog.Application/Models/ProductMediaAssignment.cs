using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>انتساب مات رسانه روی محصول؛ باینری ذخیره نمی‌شود.</summary>
public sealed record ProductMediaAssignment(
    Guid MediaAssetId,
    bool IsPrimary,
    int DisplayOrder,
    string? AltText);
