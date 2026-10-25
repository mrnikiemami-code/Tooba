using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;

/// <summary>
/// هویت <b>انتشار</b> یک روش در کل یک درخواست: مالک متعارف + سرویس/گزینهٔ متعارف.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why typed.</b> این کلید فقط برای یکسان‌سازی «یک بازبینی انتخاب‌شده برای هر تعهد» است؛ مقصد گروه،
/// برچسب نمایشی روش (<see cref="StorefrontCapturedCandidatePlan.MethodCode"/>) و شناسهٔ گروه ارسال
/// بخشی از هویت انتشار نیستند، چون دو مقصد گروهی مجوز دو بازبینی capture‌شدهٔ متفاوت از یک تعهد نیست.
/// </para>
/// <para>
/// <b>Owner equality is exact.</b> tenant مالک هرگز lowercase/نرمال نمی‌شود و فقط فضای خالی حذف می‌شود؛
/// مالک‌های مختلف (tenant/seller/kind) هرگز ادغام نمی‌شوند. سرویس/گزینه با همان سیاست مقایسهٔ
/// trim + <see cref="StringComparison.OrdinalIgnoreCase"/> مقایسه می‌شوند که
/// <see cref="StorefrontCapturedGroupProjectionComposer"/> برای پذیرش تعهد پاسخ مالک هم به کار می‌برد؛
/// کد گزینهٔ غایب همان هویت غایب است.
/// </para>
/// </remarks>
internal sealed class StorefrontCapturedPublicationBinding(StorefrontCapturedCandidatePlan candidate)
    : IEquatable<StorefrontCapturedPublicationBinding>
{
    /// <summary>سیاست یگانهٔ متعارف‌سازی متن هویت: trim و تبدیل تهی/فضاخالی به تهی.</summary>
    internal static string? Canonical(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal ShippingCapturedQuoteOwnerKind OwnerKind { get; } = candidate.OwnerKind;

    internal string? OwnerTenantId { get; } = Canonical(candidate.OwnerTenantId);

    internal Guid? OwnerSellerPartyId { get; } = candidate.OwnerSellerPartyId;

    internal string? ServiceCode { get; } = Canonical(candidate.ServiceCode);

    internal string? OptionCode { get; } = Canonical(candidate.OptionCode);

    public bool Equals(StorefrontCapturedPublicationBinding? other) =>
        other is not null
        && OwnerKind == other.OwnerKind
        && string.Equals(OwnerTenantId, other.OwnerTenantId, StringComparison.Ordinal)
        && OwnerSellerPartyId == other.OwnerSellerPartyId
        && string.Equals(ServiceCode, other.ServiceCode, StringComparison.OrdinalIgnoreCase)
        && string.Equals(OptionCode, other.OptionCode, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) =>
        obj is StorefrontCapturedPublicationBinding other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        OwnerKind,
        OwnerTenantId is null ? 0 : StringComparer.Ordinal.GetHashCode(OwnerTenantId),
        OwnerSellerPartyId,
        ServiceCode is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(ServiceCode),
        OptionCode is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(OptionCode));

    public override string ToString() =>
        $"{(int)OwnerKind}/{OwnerTenantId ?? "-"}/{OwnerSellerPartyId?.ToString("N") ?? "-"}/{ServiceCode ?? "-"}/{OptionCode ?? "-"}";
}
