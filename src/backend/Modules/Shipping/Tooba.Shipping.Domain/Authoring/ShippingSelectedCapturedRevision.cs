using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>وضعیت صریح یک اشاره‌گر انتخاب: بدون capture انتخاب‌شده یا با یک capture مشخص.</summary>
public enum ShippingSelectedCaptureState
{
    /// <summary>اشاره‌گر وجود دارد ولی هیچ capture‌ای انتخاب نشده است (پاک‌شده، نه حذف‌شده).</summary>
    Cleared = 0,

    /// <summary>یک capture تغییرناپذیر دقیق انتخاب شده است.</summary>
    Selected = 1,
}

/// <summary>
/// اشاره‌گر نسخه‌دار Shipping به <b>یک</b> capture تغییرناپذیر انتخاب‌شده برای مالک + سرویس/گزینه.
/// </summary>
/// <remarks>
/// <para>
/// <b>چه چیزی را اشاره می‌کند.</b> فقط یک ردیف <see cref="ShippingAuthoringRevisionSnapshot"/> موجود؛
/// هرگز پیش‌نویس جاری، «آخرین» یا «فعال». یک ویرایش بعدی پیش‌نویس به‌تنهایی هیچ چیز انتخاب‌شده را
/// تغییر نمی‌دهد.
/// </para>
/// <para>
/// <b>نسخه هرگز صفر نمی‌شود.</b> انتخاب اولین نسخه ۱ است، هر تغییر واقعی یک واحد بالا می‌رود و پاک‌کردن
/// ردیف/نسخه را حذف یا صفر نمی‌کند (بدون ABA)؛ انتخاب دوبارهٔ همان هدف no-op است.
/// </para>
/// <para>
/// <b>کلید متعارف در دیتابیس.</b> سرویس/گزینه به‌صورت trim + lowercase پایدار ذخیره می‌شوند، پس تغییر
/// بزرگی/کوچکی حروف نمی‌تواند اشاره‌گر دوم بسازد.
/// </para>
/// <para>
/// <b>انتخاب ≠ آمادگی.</b> این ردیف فقط «قصد انتشار» است: نه فعال‌بودن، نه صلاحیت مشتری، نه قابل‌قیمت‌بودن.
/// </para>
/// </remarks>
public sealed class ShippingSelectedCapturedRevision
{
    /// <summary>نسخهٔ اولین انتخاب ممکن.</summary>
    public const long InitialSelectionVersion = 1;

    private ShippingSelectedCapturedRevision()
    {
    }

    /// <summary>شناسهٔ پایدار همین اشاره‌گر.</summary>
    public Guid SelectionId { get; private set; }

    /// <summary>گونهٔ مالک (بخشی از کلید یکتایی).</summary>
    public ShippingDraftOwnerKind OwnerKind { get; private set; }

    /// <summary>tenant مالک Store یا تهی (بخشی از کلید یکتایی).</summary>
    public string? OwnerTenantId { get; private set; }

    /// <summary>فروشندهٔ مالک Seller یا تهی (بخشی از کلید یکتایی).</summary>
    public Guid? OwnerSellerPartyId { get; private set; }

    /// <summary>کلید متعارف سرویس (trim + lowercase).</summary>
    public string BindingServiceKey { get; private set; } = string.Empty;

    /// <summary>کلید متعارف گزینه یا تهی.</summary>
    public string? BindingOptionKey { get; private set; }

    /// <summary>capture انتخاب‌شده یا تهی در حالت پاک‌شده.</summary>
    public Guid? SelectedSnapshotId { get; private set; }

    /// <summary>نسخهٔ همزمانی واقعی؛ هرگز صفر یا کاهشی نیست.</summary>
    public long SelectionVersion { get; private set; }

    /// <summary>زمان UTC آخرین تغییر واقعی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>وضعیت صریح.</summary>
    public ShippingSelectedCaptureState State =>
        SelectedSnapshotId is null ? ShippingSelectedCaptureState.Cleared : ShippingSelectedCaptureState.Selected;

    /// <summary>کلید متعارف متن هویت: trim و lowercase پایدار؛ تهی/فضاخالی تهی می‌ماند.</summary>
    /// <param name="value">مقدار خام.</param>
    public static string? CanonicalKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    /// <summary>
    /// ردیف اشاره‌گر تازه با اولین capture انتخاب‌شده می‌سازد (نسخهٔ ۱).
    /// </summary>
    /// <param name="selectionId">شناسهٔ پایدار ردیف.</param>
    /// <param name="owner">مالک ماندگاری سازگار.</param>
    /// <param name="serviceCode">کد سرویس استخراج‌شده از payload تأییدشده.</param>
    /// <param name="optionCode">گزینهٔ استخراج‌شده از payload یا تهی.</param>
    /// <param name="snapshotId">capture انتخاب‌شده.</param>
    /// <param name="now">زمان UTC.</param>
    public static ShippingSelectedCapturedRevision CreateSelected(
        Guid selectionId,
        ShippingDraftOwner owner,
        string serviceCode,
        string? optionCode,
        Guid snapshotId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (selectionId == Guid.Empty || snapshotId == Guid.Empty)
        {
            throw Reject();
        }

        var serviceKey = CanonicalKey(serviceCode) ?? throw Reject();
        var optionKey = CanonicalKey(optionCode);

        return new ShippingSelectedCapturedRevision
        {
            SelectionId = selectionId,
            OwnerKind = owner.Kind,
            OwnerTenantId = owner.TenantId,
            OwnerSellerPartyId = owner.SellerPartyId,
            BindingServiceKey = serviceKey,
            BindingOptionKey = optionKey,
            SelectedSnapshotId = snapshotId,
            SelectionVersion = InitialSelectionVersion,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// همان capture را انتخاب می‌کند؛ انتخاب دوبارهٔ همان هدف هیچ ستونی را نمی‌نویسد.
    /// </summary>
    /// <param name="snapshotId">capture تأییدشده.</param>
    /// <param name="now">زمان UTC.</param>
    /// <returns>آیا تغییری نوشته شد.</returns>
    public bool Select(Guid snapshotId, DateTimeOffset now)
    {
        if (snapshotId == Guid.Empty)
        {
            throw Reject();
        }

        if (SelectedSnapshotId == snapshotId)
        {
            return false;
        }

        SelectedSnapshotId = snapshotId;
        SelectionVersion = NextVersion(SelectionVersion);
        UpdatedAt = now;
        return true;
    }

    /// <summary>
    /// capture انتخاب‌شده را پاک می‌کند؛ ردیف و نسخه باقی می‌مانند و پاک‌کردن دوباره no-op است.
    /// </summary>
    /// <param name="now">زمان UTC.</param>
    /// <returns>آیا تغییری نوشته شد.</returns>
    public bool Clear(DateTimeOffset now)
    {
        if (SelectedSnapshotId is null)
        {
            return false;
        }

        SelectedSnapshotId = null;
        SelectionVersion = NextVersion(SelectionVersion);
        UpdatedAt = now;
        return true;
    }

    private static long NextVersion(long current) =>
        current <= 0 || current == long.MaxValue ? throw Reject() : current + 1;

    private static SemanticException Reject() =>
        new(new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
}
