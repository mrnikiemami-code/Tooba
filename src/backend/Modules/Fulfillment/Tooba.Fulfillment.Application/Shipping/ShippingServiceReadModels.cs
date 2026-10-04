using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>ردیف لیست سرویس ارسال (شکل JSON پایدار).</summary>
public sealed record ShippingServiceListItemDto(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    string Name,
    bool IsActive,
    int SortOrder,
    int OptionCount,
    int ActiveOptionCount);

/// <summary>ترجمهٔ سرویس ارسال والد.</summary>
public sealed record ShippingServiceTranslationDto(Guid LanguageId, string Name, string? Description);

/// <summary>ترجمهٔ نوع سرویس فرزند.</summary>
public sealed record ShippingServiceOptionTranslationDto(Guid LanguageId, string Name);

/// <summary>جزئیات گزینهٔ سطح ۲.</summary>
public sealed record ShippingServiceOptionDetailDto(
    Guid ShippingServiceOptionId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceOptionTranslationDto> Translations);

/// <summary>جزئیات سرویس ارسال (شکل JSON پایدار).</summary>
public sealed record ShippingServiceDetailDto(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingServiceTranslationDto> Translations,
    IReadOnlyList<ShippingServiceOptionDetailDto> Options);
