using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>گزینهٔ محور تنوع در ویرایشگر.</summary>
public sealed record ProductVariantAxisOption(
    Guid OptionId,
    string LocalizedLabel,
    string Code,
    bool IsActive);
