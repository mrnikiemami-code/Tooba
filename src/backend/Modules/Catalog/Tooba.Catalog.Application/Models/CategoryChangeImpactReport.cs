using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// گزارش انسانی تأثیر تغییر رده برای تأیید Admin (T036-P).
/// فیلدهای اختیاری انتهایی برای سازگاری با مصرف‌کنندگان قبلی پیش‌فرض دارند.
/// </summary>
public sealed record CategoryChangeImpactReport(
    Guid ProductId,
    Guid NewCategoryId,
    int CompatiblePreservedCount,
    int OrphanCount,
    int NewlyRequiredMissingCount,
    IReadOnlyList<CategoryChangeOrphanSummary> OrphanSummaries,
    IReadOnlyList<string> NewlyRequiredLabels,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds,
    string MessageFa,
    int ImpactedVariantCount = 0,
    string? VariantImpactMessageFa = null,
    Guid? CurrentCategoryId = null,
    string? CurrentCategoryPath = null,
    string? TargetCategoryPath = null,
    IReadOnlyList<string>? PreservedAttributes = null,
    IReadOnlyList<string>? AddedAttributes = null,
    IReadOnlyList<string>? RemovedAttributes = null,
    IReadOnlyList<string>? RequiredMissing = null,
    bool VariantCompatible = true,
    int PreservedVariantCount = 0,
    int AffectedVariantCount = 0,
    bool AdditionalMembershipPromoted = false,
    int OtherDisplayMembershipsRemainCount = 0,
    IReadOnlyList<string>? ReadinessBlockers = null)
{
    /// <summary>شناسهٔ دستهٔ هدف؛ همان NewCategoryId.</summary>
    public Guid TargetCategoryId => NewCategoryId;
}
