using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;

namespace Tooba.Shipping.Infrastructure.Scheduling;

/// <summary>
/// درز Contracts برای خواندن ورودی‌های زمان‌بندی یک <b>تلاش فیزیکی جدید</b> از یک بازبینی
/// <b>capture‌شدهٔ دقیق</b>: هویت مالک/پیش‌نویس/بازبینی، همان گروه/ردیف پذیرفته‌شده و همان هفته‌های ذخیره‌شده.
/// </summary>
/// <remarks>
/// <para>
/// <b>One authority.</b> این آداپتور هیچ قاعدهٔ زمانی نمی‌سازد و هیچ تقویمی اعتبارسنجی نمی‌کند؛ فقط
/// واقعیت‌های ذخیره‌شده را lossless به Contracts نگاشت می‌کند تا مرجع موجود
/// <see cref="ShippingDispatchSchedulingAuthority"/> همان‌ها را مصرف کند. هیچ JSON round-trip یا کپی
/// گم‌کننده در کار نیست.
/// </para>
/// <para>
/// <b>Identity first.</b> اول انطباق گونهٔ مالک با زمینهٔ نصب/تجارت جاری، بعد خواندن <em>دقیق</em> بازبینی
/// با کلید مالک، بعد انطباق شناسهٔ capture/خدمت/گزینه و سپس حل دقیق همان گروه و همان ردیف. نه latest،
/// نه current، نه active، نه «اولین تطابق».
/// </para>
/// <para>
/// <b>No pricing.</b> موتور نرخ، Geography، مقصد، وزن/تعداد/مبلغ، ارز، provider و انتخاب نرخ هیچ‌کدام
/// صدا زده نمی‌شوند. خواندن نرخ برای قیمت‌گذاری مجاز نیست؛ خواندن همان ردیف پذیرفته‌شده برای برداشتن
/// مقادیر زمانی ذخیره‌شدهٔ آن مجاز است.
/// </para>
/// <para>
/// <b>Read-only.</b> هیچ نوشتن/SaveChanges/تراکنش/capture نیست و استثنا یا لغو فراخوان عیناً منتشر
/// می‌شود؛ تنها بدنهٔ ناسازگار همان ردیف (خطای معنایی canonical خواندن payload) به حکم صریح تبدیل
/// می‌شود و هیچ شکست بیرونی «نیست» نامیده نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingAttemptSchedulingLookup(
    IShippingAuthoringDraftDirectory drafts,
    ICurrentEdition edition,
    ICurrentTenant tenant,
    ILogger<ShippingAttemptSchedulingLookup> logger) : IShippingAttemptSchedulingLookup
{
    /// <inheritdoc />
    public async Task<ShippingAttemptSchedulingResult> LookupAsync(
        ShippingAttemptSchedulingLookupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var owner = ResolveOwnerScope(request);
        if (owner is null)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.OwnerScopeUnavailable,
                ShippingAttemptSchedulingReasonCodes.OwnerScopeUnavailable);
        }

        // EXACT identity (owner + draft + revision). No newest/current/active fallback exists here either.
        var captured = await drafts.GetCapturedRevisionAsync(
            owner,
            request.DraftId,
            request.SourceRevision,
            cancellationToken);
        if (!captured.IsSuccess)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable,
                ShippingAttemptSchedulingReasonCodes.CapturedRevisionUnavailable);
        }

        var snapshot = captured.Value;

        ShippingAuthoringRevisionPayload payload;
        try
        {
            // The row headers are the authority; a body describing another identity is corrupt, not a read.
            payload = snapshot.ReadPayload();
        }
        catch (SemanticException)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable,
                ShippingAttemptSchedulingReasonCodes.CapturedRevisionUnavailable);
        }

        // A caller that already bound an exact capture must be answered about THAT row, never another one.
        if (request.ExpectedSnapshotId is { } expectedSnapshotId
            && expectedSnapshotId != snapshot.SnapshotId)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.SnapshotMismatch,
                ShippingAttemptSchedulingReasonCodes.SnapshotMismatch);
        }

        // Same canonical comparison the captured quote authority uses over the same payload: the STORED
        // service/option are trimmed and compared case-insensitively; an absent option is null, not "".
        var capturedServiceCode = payload.ServiceCode?.Trim() ?? string.Empty;
        var capturedOptionCode = string.IsNullOrWhiteSpace(payload.OptionCode)
            ? null
            : payload.OptionCode.Trim();
        var requestedServiceCode = request.ExpectedServiceCode?.Trim() ?? string.Empty;
        var requestedOptionCode = string.IsNullOrWhiteSpace(request.ExpectedOptionCode)
            ? null
            : request.ExpectedOptionCode.Trim();
        if (!string.Equals(capturedServiceCode, requestedServiceCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(capturedOptionCode, requestedOptionCode, StringComparison.OrdinalIgnoreCase))
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.ServiceOptionMismatch,
                ShippingAttemptSchedulingReasonCodes.ServiceOptionMismatch);
        }

        // Exactly the supplied group; several rows with one id is a data conflict, never a first match.
        var groups = payload.Groups
            .Where(group => group.GroupId == request.GroupId)
            .ToArray();
        if (groups.Length == 0)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.GroupUnavailable,
                ShippingAttemptSchedulingReasonCodes.GroupUnavailable);
        }

        if (groups.Length > 1)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.GroupAmbiguous,
                ShippingAttemptSchedulingReasonCodes.GroupAmbiguous);
        }

        var group = groups[0];

        // Exactly the supplied rate row INSIDE that group; no inference from ranges, price, geography or order.
        var rows = group.RateRows
            .Where(row => row.RateRowId == request.RateRowId)
            .ToArray();
        if (rows.Length == 0)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.RateRowUnavailable,
                ShippingAttemptSchedulingReasonCodes.RateRowUnavailable);
        }

        if (rows.Length > 1)
        {
            return Refuse(
                ShippingAttemptSchedulingVerdict.RateRowAmbiguous,
                ShippingAttemptSchedulingReasonCodes.RateRowAmbiguous);
        }

        var row = rows[0];
        var settings = payload.MethodSettings;

        return new ShippingAttemptSchedulingResult(
            ShippingAttemptSchedulingVerdict.CapturedInputs,
            ShippingAttemptSchedulingReasonCodes.CapturedInputs,
            new ShippingAttemptSchedulingInputs(
                MapOwnerKind(snapshot.OwnerKind),
                snapshot.OwnerTenantId,
                snapshot.OwnerSellerPartyId,
                snapshot.SnapshotId,
                snapshot.DraftId,
                snapshot.SourceRevision,
                snapshot.FormatVersion,
                capturedServiceCode,
                capturedOptionCode,
                group.GroupId,
                row.RateRowId,
                row.MinDeliveryDays,
                row.MaxDeliveryDays,
                settings?.PreparationMinutes,
                settings?.HandoffMinutes,
                settings?.PreparationMode ?? ShippingAuthoringPreparationProcessingMode.Unspecified,
                MapReceivingWeek(payload.WeeklySchedule),
                MapWeek(payload.CarrierAcceptance),
                MapWeek(payload.ProcessingHours),
                MissingInputs(payload)));
    }

    /// <summary>
    /// گونهٔ مالک درخواستی را با زمینهٔ trusted همین درخواست می‌سنجد؛ زمینهٔ ناقص/ناسازگار همیشه
    /// <c>null</c> است و هیچ مقداری پیش‌فرض نمی‌شود.
    /// </summary>
    /// <param name="request">درخواست Contracts.</param>
    private ShippingAuthoringOwnerScope? ResolveOwnerScope(ShippingAttemptSchedulingLookupRequest request)
        => ShippingCapturedOwnerScopeResolver.Resolve(
            request.OwnerKind,
            request.OwnerTenantId,
            request.OwnerSellerPartyId,
            edition,
            tenant);

    /// <summary>
    /// گونهٔ مالک ذخیره‌شده را به گونهٔ Contracts نگاشت می‌کند؛ یک نگاشت صریح، بدون cast عددی.
    /// </summary>
    /// <param name="kind">گونهٔ مالک ذخیره‌شده.</param>
    private static ShippingCapturedQuoteOwnerKind MapOwnerKind(ShippingDraftOwnerKind kind)
        => kind switch
        {
            ShippingDraftOwnerKind.Store => ShippingCapturedQuoteOwnerKind.Store,
            ShippingDraftOwnerKind.Seller => ShippingCapturedQuoteOwnerKind.Seller,
            _ => ShippingCapturedQuoteOwnerKind.Marketplace,
        };

    /// <summary>
    /// هفتهٔ دریافت مشتری را با همان روزها/ساعت‌ها/مدت نوبت ذخیره‌شده نگاشت می‌کند؛ غیبت هفته همان
    /// <c>null</c> است و هفتهٔ ساختگی ساخته نمی‌شود.
    /// </summary>
    /// <param name="week">هفتهٔ ذخیره‌شده یا تهی.</param>
    private static ShippingAttemptReceivingWeekView? MapReceivingWeek(ShippingAuthoringRevisionWeek? week)
        => week is null
            ? null
            : new ShippingAttemptReceivingWeekView(
                week.TimeZoneId,
                week.Days
                    .Select(day => new ShippingSchedulingDayView(
                        day.Weekday,
                        day.IsWorking,
                        day.StartTime,
                        day.EndTime))
                    .ToArray(),
                week.SlotDurationMinutes);

    /// <summary>
    /// یک هفتهٔ پذیرش/پردازش را با همان پرچم‌ها، روزهای ISO و ساعت‌های محلی نگاشت می‌کند. دو نوع ذخیره‌شده
    /// شکل یکسانی دارند و هر دو بازهٔ نیم‌باز <c>[open, close)</c> هستند.
    /// </summary>
    /// <param name="week">هفتهٔ پذیرش حامل ذخیره‌شده یا تهی.</param>
    private static ShippingSchedulingWeekView? MapWeek(ShippingAuthoringRevisionCarrierWeek? week)
        => week is null
            ? null
            : new ShippingSchedulingWeekView(
                week.TimeZoneId,
                week.Days
                    .Select(day => new ShippingSchedulingDayView(
                        day.Weekday,
                        day.IsWorking,
                        day.OpensAt,
                        day.ClosesAt))
                    .ToArray());

    /// <summary>
    /// هفتهٔ کاری پردازش را با همان پرچم‌ها، روزهای ISO و ساعت‌های محلی نگاشت می‌کند.
    /// </summary>
    /// <param name="week">هفتهٔ پردازش ذخیره‌شده یا تهی.</param>
    private static ShippingSchedulingWeekView? MapWeek(ShippingAuthoringRevisionProcessingWeek? week)
        => week is null
            ? null
            : new ShippingSchedulingWeekView(
                week.TimeZoneId,
                week.Days
                    .Select(day => new ShippingSchedulingDayView(
                        day.Weekday,
                        day.IsWorking,
                        day.OpensAt,
                        day.ClosesAt))
                    .ToArray());

    /// <summary>
    /// کدهای پایدار ورودی‌هایی که در همین capture <b>ذخیره نشده‌اند</b> را می‌سازد.
    /// </summary>
    /// <remarks>
    /// این فهرست فقط غیبت واقعیت ذخیره‌شده را می‌گوید؛ هیچ نتیجه‌ای دربارهٔ ساختنی بودن یک برنامه، پوشش
    /// مقصد، فعال بودن روش یا کفایت یک قالب تاریخی نمی‌دهد.
    /// </remarks>
    /// <param name="payload">بدنهٔ تأییدشدهٔ همان capture.</param>
    private static IReadOnlyList<string> MissingInputs(ShippingAuthoringRevisionPayload payload)
    {
        var missing = new List<string>(6);

        if (payload.MethodSettings is not { } settings)
        {
            missing.Add(ShippingAttemptSchedulingInputCodes.MethodSettings);
        }
        else if (settings.PreparationMinutes is null || settings.HandoffMinutes is null)
        {
            missing.Add(ShippingAttemptSchedulingInputCodes.PreparationHandoffMinutes);
        }

        if (payload.MethodSettings?.PreparationMode is null
            or ShippingAuthoringPreparationProcessingMode.Unspecified)
        {
            missing.Add(ShippingAttemptSchedulingInputCodes.ProcessingMode);
        }

        if (payload.WeeklySchedule is null)
        {
            missing.Add(ShippingAttemptSchedulingInputCodes.ReceivingWeek);
        }

        if (payload.CarrierAcceptance is null)
        {
            missing.Add(ShippingAttemptSchedulingInputCodes.CarrierWeek);
        }

        if (payload.ProcessingHours is null)
        {
            missing.Add(ShippingAttemptSchedulingInputCodes.ProcessingWeek);
        }

        return missing;
    }

    /// <summary>
    /// یک امتناع <b>تعریف‌شده</b> می‌سازد: هیچ بدنه‌ای، هیچ provenance‌ای و هیچ استثنایی برای فراخوان.
    /// </summary>
    /// <param name="verdict">حکم امتناع.</param>
    /// <param name="reasonCode">کد دلیل پایدار.</param>
    private ShippingAttemptSchedulingResult Refuse(
        ShippingAttemptSchedulingVerdict verdict,
        string reasonCode)
    {
        logger.LogDebug("Attempt scheduling lookup refused with {Verdict}.", verdict);
        return ShippingAttemptSchedulingResult.Refused(verdict, reasonCode);
    }
}
