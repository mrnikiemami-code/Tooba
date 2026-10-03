using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>گزینهٔ شمارشی برای ویرایشگر ویژگی.</summary>
public sealed record ProductAttributeEditorOption(
    Guid OptionId,
    string LocalizedLabel,
    bool IsActive);
