using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>محور انتخاب‌شده با گزینه‌های محصول.</summary>
public sealed record ProductVariantSelectedAxisInput(
    Guid DefinitionId,
    IReadOnlyList<Guid> OptionIds);
