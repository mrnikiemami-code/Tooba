namespace Tooba.PageComposition.Endpoints.Models;

/// <summary>بدنهٔ مرتب‌سازی sectionها.</summary>
public sealed record ReorderHomeSectionsBody(IReadOnlyList<Guid> SectionIds);

/// <summary>بدنهٔ افزودن section.</summary>
public sealed record AddHomeSectionBody(
    string SectionType,
    string? Variant,
    string? ConfigurationJson,
    bool IsVisible = true);

/// <summary>بدنهٔ به‌روزرسانی section.</summary>
public sealed record UpdateHomeSectionBody(
    bool? IsVisible,
    string? ConfigurationJson,
    string? Variant);
