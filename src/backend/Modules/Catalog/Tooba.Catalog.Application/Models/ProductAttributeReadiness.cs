using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>آمادگی مقادیر ویژگی برای انتشار بعدی.</summary>
public sealed record ProductAttributeReadiness(
    bool IsComplete,
    IReadOnlyList<string> MissingRequiredCodes,
    IReadOnlyList<string> InvalidValues);
