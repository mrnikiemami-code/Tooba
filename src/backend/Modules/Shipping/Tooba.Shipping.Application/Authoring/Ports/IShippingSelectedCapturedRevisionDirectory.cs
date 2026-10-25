using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Application.Authoring.Ports;

/// <summary>
/// درز نوشتن اشاره‌گر انتخاب capture برای مالک + Binding — داخلی Shipping، بدون قرارداد عمومی/HTTP.
/// </summary>
/// <remarks>
/// <para>
/// این درز فقط انتخاب/پاک‌کردن <b>قصد</b> را می‌نویسد؛ هرگز خودش capture نمی‌سازد و هرگز پیش‌نویس را
/// تغییر نمی‌دهد. هدف همیشه یک capture <b>موجود</b> و دقیقاً متعلق به همین مالک است.
/// </para>
/// <para>
/// <paramref name="expectedSelectionVersion"/> شرط همزمانی واقعی است: صفر یعنی هنوز ردیفی نیست؛ نسخهٔ
/// کهنه همیشه تعارض پایدار می‌دهد و هیچ last-write-wins یا تلاش دوبارهٔ خودکار وجود ندارد.
/// </para>
/// </remarks>
public interface IShippingSelectedCapturedRevisionDirectory
{
    /// <summary>یک capture موجود را به‌عنوان انتخاب‌شده ثبت می‌کند.</summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="serviceCode">کد سرویس درخواستی.</param>
    /// <param name="optionCode">گزینهٔ درخواستی یا تهی.</param>
    /// <param name="draftId">پیش‌نویس مبدأ capture موجود.</param>
    /// <param name="sourceRevision">بازبینی مبدأ همان capture.</param>
    /// <param name="expectedSelectionVersion">نسخهٔ مورد انتظار اشاره‌گر (صفر = بدون ردیف).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingSelectedCaptureSelection>> SelectAsync(
        ShippingAuthoringOwnerScope owner,
        string serviceCode,
        string? optionCode,
        Guid draftId,
        long sourceRevision,
        long expectedSelectionVersion,
        CancellationToken cancellationToken);

    /// <summary>capture انتخاب‌شده را پاک می‌کند و نسخه را (در صورت تغییر) یک واحد بالا می‌برد.</summary>
    /// <param name="owner">مالک مجاز همین درخواست.</param>
    /// <param name="serviceCode">کد سرویس درخواستی.</param>
    /// <param name="optionCode">گزینهٔ درخواستی یا تهی.</param>
    /// <param name="expectedSelectionVersion">نسخهٔ مورد انتظار اشاره‌گر (صفر = بدون ردیف).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingSelectedCaptureSelection>> ClearAsync(
        ShippingAuthoringOwnerScope owner,
        string serviceCode,
        string? optionCode,
        long expectedSelectionVersion,
        CancellationToken cancellationToken);
}

/// <summary>
/// وضعیت پایدار یک اشاره‌گر پس از یک عملیات نوشتن؛ <see cref="Exists"/> تهی یعنی هیچ ردیفی نیست.
/// </summary>
/// <param name="Exists">آیا ردیف اشاره‌گر وجود دارد.</param>
/// <param name="State">وضعیت صریح.</param>
/// <param name="SelectionVersion">نسخهٔ ذخیره‌شده.</param>
/// <param name="SnapshotId">capture انتخاب‌شده.</param>
/// <param name="DraftId">پیش‌نویس مبدأ capture.</param>
/// <param name="SourceRevision">بازبینی مبدأ capture.</param>
public sealed record ShippingSelectedCaptureSelection(
    bool Exists,
    ShippingSelectedCaptureState State,
    long? SelectionVersion,
    Guid? SnapshotId,
    Guid? DraftId,
    long? SourceRevision);
