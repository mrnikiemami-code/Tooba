using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// پرچم‌های رفتار category-specific روی assignment.
/// </summary>
public sealed record CategoryAttributeAssignmentFlags(
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable);
