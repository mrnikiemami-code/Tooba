using System.Text.Json;
using System.Text.Json.Serialization;
using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// هویت مالک همان‌گونه که در لحظهٔ capture ذخیره شده بود؛ یک مقدار تغییرناپذیر، نه مجوز.
/// </summary>
/// <param name="Kind">گونهٔ مالک ذخیره‌شده.</param>
/// <param name="TenantId">tenant مالک Store یا تهی.</param>
/// <param name="SellerPartyId">شناسهٔ فروشندهٔ مالک Seller یا تهی.</param>
public sealed record ShippingAuthoringRevisionOwner(
    ShippingDraftOwnerKind Kind,
    string? TenantId,
    Guid? SellerPartyId);

/// <summary>یک انتخاب جغرافیایی همان‌گونه که ذخیره شده بود (هویت گره، بدون بازخوانی کاتالوگ).</summary>
/// <param name="SelectedAreaId">شناسهٔ پایدار ردیف انتخاب.</param>
/// <param name="NodeKind">گونهٔ گرهٔ ذخیره‌شده.</param>
/// <param name="NodeId">شناسهٔ پایدار گرهٔ ذخیره‌شده.</param>
public sealed record ShippingAuthoringRevisionArea(
    Guid SelectedAreaId,
    ShippingDestinationNodeKind NodeKind,
    Guid NodeId);

/// <summary>عنوان ذخیره‌شدهٔ یک گروه برای یک زبان محتوا.</summary>
/// <param name="LanguageId">شناسهٔ زبان محتوا.</param>
/// <param name="Title">متن ذخیره‌شده.</param>
public sealed record ShippingAuthoringRevisionGroupTitle(Guid LanguageId, string Title);

/// <summary>
/// یک ردیف نرخ با <b>تمام</b> کران‌ها و هر دو مؤلفهٔ هزینه؛ هیچ فیلدی حذف یا گرد نمی‌شود.
/// </summary>
/// <param name="RateRowId">شناسهٔ پایدار ردیف.</param>
/// <param name="MinWeightGrams">کران پایین وزن.</param>
/// <param name="MaxWeightGrams">کران بالای وزن.</param>
/// <param name="MinItemCount">کران پایین تعداد.</param>
/// <param name="MaxItemCount">کران بالای تعداد.</param>
/// <param name="MinMerchandiseAmount">کران پایین مبلغ کالا.</param>
/// <param name="MaxMerchandiseAmount">کران بالای مبلغ کالا.</param>
/// <param name="FreightAmount">هزینهٔ حمل ذخیره‌شده.</param>
/// <param name="PackagingAmount">هزینهٔ بسته‌بندی ذخیره‌شده.</param>
/// <param name="MinDeliveryDays">کمینهٔ مدت تحویل.</param>
/// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل.</param>
public sealed record ShippingAuthoringRevisionRateRow(
    Guid RateRowId,
    long? MinWeightGrams,
    long? MaxWeightGrams,
    int? MinItemCount,
    int? MaxItemCount,
    decimal? MinMerchandiseAmount,
    decimal? MaxMerchandiseAmount,
    decimal FreightAmount,
    decimal PackagingAmount,
    int MinDeliveryDays,
    int MaxDeliveryDays);

/// <summary>یک گروه منطقه‌ای با ترتیب، ارز bind‌شده، انتخاب‌ها، ردیف‌ها و عنوان‌های همان گروه.</summary>
/// <param name="GroupId">شناسهٔ پایدار گروه.</param>
/// <param name="Order">ترتیب ذخیره‌شده.</param>
/// <param name="CurrencyCode">ارز bind‌شده یا تهی.</param>
/// <param name="Areas">انتخاب‌های جغرافیایی.</param>
/// <param name="RateRows">ردیف‌های نرخ.</param>
/// <param name="Titles">عنوان‌های زبانی.</param>
public sealed record ShippingAuthoringRevisionGroup(
    Guid GroupId,
    int Order,
    string? CurrencyCode,
    IReadOnlyList<ShippingAuthoringRevisionArea> Areas,
    IReadOnlyList<ShippingAuthoringRevisionRateRow> RateRows,
    IReadOnlyList<ShippingAuthoringRevisionGroupTitle> Titles);

/// <summary>نام/توضیح ذخیره‌شدهٔ یک زبان (تهی و رشتهٔ خالی از هم جدا می‌مانند).</summary>
/// <param name="LanguageId">شناسهٔ زبان.</param>
/// <param name="Name">نام ذخیره‌شده.</param>
/// <param name="Description">توضیح ذخیره‌شده یا تهی.</param>
public sealed record ShippingAuthoringRevisionTranslation(Guid LanguageId, string Name, string? Description);

/// <summary>تنظیمات روش ذخیره‌شده؛ تهی در ریشه یعنی «تنظیم‌نشده»، نه پیش‌فرض.</summary>
/// <param name="FreightPaymentMode">زمان پرداخت کرایه.</param>
/// <param name="CalculationMode">حالت محاسبه.</param>
/// <param name="UpdatedAt">زمان آخرین نوشتن تنظیمات.</param>
public sealed record ShippingAuthoringRevisionSettings(
    ShippingFreightPayerMode FreightPaymentMode,
    ShippingAuthoringCalculationMode CalculationMode,
    DateTimeOffset UpdatedAt);

/// <summary>یک روز برنامهٔ ذخیره‌شده، شامل ساعت‌های به‌یاد‌ماندهٔ روز تعطیل.</summary>
/// <param name="Weekday">روز هفتهٔ ISO.</param>
/// <param name="IsWorking">پرچم کاری ذخیره‌شده.</param>
/// <param name="StartTime">ساعت شروع ذخیره‌شدهٔ همان روز یا تهی.</param>
/// <param name="EndTime">ساعت پایان ذخیره‌شدهٔ همان روز یا تهی.</param>
public sealed record ShippingAuthoringRevisionWeekDay(
    int Weekday,
    bool IsWorking,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

/// <summary>برنامهٔ هفتگی ذخیره‌شده با منطقهٔ زمانی و زمان آخرین نوشتن.</summary>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی ذخیره‌شده.</param>
/// <param name="UpdatedAt">زمان آخرین نوشتن برنامه.</param>
/// <param name="Days">دقیقاً هفت روز ذخیره‌شده.</param>
/// <param name="SlotDurationMinutes">TB-241: مدت اختیاری هر نوبت تحویل به دقیقه یا تهی.</param>
public sealed record ShippingAuthoringRevisionWeek(
    string TimeZoneId,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ShippingAuthoringRevisionWeekDay> Days,
    int? SlotDurationMinutes = null);

/// <summary>
/// بدنهٔ تایپ‌شدهٔ یک capture کامل از بازبینی پیش‌نویس. این record فقط واقعیت‌های <b>ذخیره‌شدهٔ</b>
/// همان aggregate است: هیچ کاتالوگ/جغرافیا/زبان یا فکت تازه در capture خوانده نمی‌شود.
/// </summary>
/// <param name="FormatVersion">نسخهٔ قالب این بدنه؛ رمزگشای ناشناخته رد می‌شود.</param>
/// <param name="Owner">هویت مالک ذخیره‌شده.</param>
/// <param name="ServiceCode">کد خدمت Binding.</param>
/// <param name="OptionCode">گزینهٔ Binding یا تهی.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="SourceRevision">شمارهٔ بازبینی مبدأ.</param>
/// <param name="DraftCreatedAt">زمان ایجاد پیش‌نویس.</param>
/// <param name="DraftUpdatedAt">زمان آخرین تغییر پیش‌نویس.</param>
/// <param name="ShippingTypeId">شناسهٔ نوع انتخاب‌شده یا تهی (خام، بدون بازخوانی کاتالوگ).</param>
/// <param name="Groups">گروه‌های منطقه‌ای به ترتیب ذخیره‌شده.</param>
/// <param name="Translations">نام/توضیح هر زبان.</param>
/// <param name="MethodSettings">تنظیمات روش یا تهی.</param>
/// <param name="WeeklySchedule">برنامهٔ هفتگی یا تهی.</param>
public sealed record ShippingAuthoringRevisionPayload(
    int FormatVersion,
    ShippingAuthoringRevisionOwner Owner,
    string ServiceCode,
    string? OptionCode,
    Guid DraftId,
    long SourceRevision,
    DateTimeOffset DraftCreatedAt,
    DateTimeOffset DraftUpdatedAt,
    Guid? ShippingTypeId,
    IReadOnlyList<ShippingAuthoringRevisionGroup> Groups,
    IReadOnlyList<ShippingAuthoringRevisionTranslation> Translations,
    ShippingAuthoringRevisionSettings? MethodSettings,
    ShippingAuthoringRevisionWeek? WeeklySchedule);

/// <summary>
/// یک capture تغییرناپذیر از <b>یک</b> بازبینی دقیق پیش‌نویس نویسندگی.
/// </summary>
/// <remarks>
/// <para>
/// <b>Staging، نه انتشار.</b> این ردیف فقط یک واقعیت تاریخی است: هیچ روشی را فعال/منتشر نمی‌کند، هیچ
/// اشاره‌گر فعال/آماده‌بودنی ندارد و هیچ مصرف‌کنندهٔ مشتری آن را نمی‌خواند.
/// </para>
/// <para>
/// <b>تغییرناپذیری.</b> پس از ساخت هیچ setter عمومی/متد تغییر وجود ندارد؛ تغییر بعدی پیش‌نویس نه این
/// ردیف را بازنویسی می‌کند و نه payload آن را. capture دوبارهٔ همان (مالک، پیش‌نویس، بازبینی) همان
/// ردیف و همان زمان capture را برمی‌گرداند.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringRevisionSnapshot
{
    /// <summary>نسخهٔ قالب بدنهٔ همین پیاده‌سازی برای capture‌های بدون مدت نوبت.</summary>
    public const int LegacyFormatVersion = 1;

    /// <summary>
    /// TB-241: نسخهٔ قالبی که مدت نوبت تحویل را نیز در بر می‌گیرد. خوانندهٔ قدیمی این نسخه را
    /// نمی‌فهمد، پس capture دارای مدت هرگز با نسخهٔ ۱ نوشته نمی‌شود.
    /// </summary>
    public const int SlotDurationFormatVersion = 2;

    /// <summary>نسخهٔ قالبی که نویسندهٔ فعلی برای capture تازه به کار می‌برد (بدون‌مدت=۱، بادمت=۲).</summary>
    public const int CurrentFormatVersion = SlotDurationFormatVersion;

    /// <summary>آیا این نسخهٔ قالب برای خواندن پشتیبانی می‌شود (فقط ۱ و ۲).</summary>
    /// <param name="formatVersion">نسخهٔ قالب.</param>
    public static bool IsSupportedFormatVersion(int formatVersion)
        => formatVersion is LegacyFormatVersion or SlotDurationFormatVersion;

    private ShippingAuthoringRevisionSnapshot()
    {
    }

    /// <summary>شناسهٔ پایدار این capture.</summary>
    public Guid SnapshotId { get; private set; }

    /// <summary>گونهٔ مالک ذخیره‌شده (کلید یکتایی).</summary>
    public ShippingDraftOwnerKind OwnerKind { get; private set; }

    /// <summary>tenant مالک Store (کلید یکتایی).</summary>
    public string? OwnerTenantId { get; private set; }

    /// <summary>شناسهٔ فروشندهٔ مالک Seller (کلید یکتایی).</summary>
    public Guid? OwnerSellerPartyId { get; private set; }

    /// <summary>پیش‌نویس مبدأ (کلید یکتایی).</summary>
    public Guid DraftId { get; private set; }

    /// <summary>شمارهٔ بازبینی مبدأ (کلید یکتایی).</summary>
    public long SourceRevision { get; private set; }

    /// <summary>نسخهٔ قالب payload.</summary>
    public int FormatVersion { get; private set; }

    /// <summary>زمان UTC ثبت این capture؛ در capture تکراری همان مقدار اولیه می‌ماند.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>بدنهٔ JSONB تایپ‌شده؛ فقط با <see cref="ShippingAuthoringRevisionSnapshotCodec"/> خوانده می‌شود.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// یک ردیف ذخیره‌شده را برای <b>خواندن</b> بازسازی می‌کند؛ هدرهای ردیف همان‌هایی هستند که در دیتابیس
    /// ذخیره شده‌اند. این متد هیچ نوشتنی نمی‌سازد و بدنه را اعتبارسنجی نمی‌کند؛ تناقض بدنه با هدر در
    /// <see cref="ReadPayload"/> رد می‌شود.
    /// </summary>
    /// <param name="snapshotId">شناسهٔ ردیف ذخیره‌شده.</param>
    /// <param name="ownerKind">گونهٔ مالک ذخیره‌شده.</param>
    /// <param name="ownerTenantId">tenant ذخیره‌شده.</param>
    /// <param name="ownerSellerPartyId">فروشندهٔ ذخیره‌شده.</param>
    /// <param name="draftId">پیش‌نویس ذخیره‌شده.</param>
    /// <param name="sourceRevision">بازبینی ذخیره‌شده.</param>
    /// <param name="formatVersion">نسخهٔ قالب ذخیره‌شده.</param>
    /// <param name="capturedAt">زمان ثبت ذخیره‌شده.</param>
    /// <param name="payload">بدنهٔ JSONB ذخیره‌شده.</param>
    public static ShippingAuthoringRevisionSnapshot RestoreForRead(
        Guid snapshotId,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        Guid draftId,
        long sourceRevision,
        int formatVersion,
        DateTimeOffset capturedAt,
        string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRevisionSnapshotInvalid));
        }

        return new ShippingAuthoringRevisionSnapshot
        {
            SnapshotId = snapshotId,
            OwnerKind = ownerKind,
            OwnerTenantId = ownerTenantId,
            OwnerSellerPartyId = ownerSellerPartyId,
            DraftId = draftId,
            SourceRevision = sourceRevision,
            FormatVersion = formatVersion,
            CapturedAt = capturedAt,
            Payload = payload,
        };
    }

    /// <summary>
    /// یک capture تازه از وضعیت فعلی همان aggregate می‌سازد. فراخوان باید پیش‌تر مالک/بازبینی را
    /// سنجیده باشد؛ این متد هیچ قاعدهٔ دسترسی‌ای اضافه نمی‌کند.
    /// </summary>
    /// <param name="draft">پیش‌نویس بارگذاری‌شده با کل گراف.</param>
    /// <param name="now">زمان UTC ثبت.</param>
    /// <param name="snapshotId">شناسهٔ قطعی اختیاری برای بازپخش.</param>
    public static ShippingAuthoringRevisionSnapshot Capture(
        ShippingAuthoringDraft draft,
        DateTimeOffset now,
        Guid? snapshotId = null)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var payload = ShippingAuthoringRevisionSnapshotCodec.FromDraft(draft);
        return new ShippingAuthoringRevisionSnapshot
        {
            SnapshotId = snapshotId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            OwnerKind = draft.OwnerKind,
            OwnerTenantId = draft.OwnerTenantId,
            OwnerSellerPartyId = draft.OwnerSellerPartyId,
            DraftId = draft.DraftId,
            SourceRevision = draft.Revision,
            FormatVersion = payload.FormatVersion,
            CapturedAt = now,
            Payload = ShippingAuthoringRevisionSnapshotCodec.Serialize(payload),
        };
    }

    /// <summary>
    /// بدنهٔ تایپ‌شدهٔ همین capture را رمزگشایی می‌کند؛ بدنهٔ ناخوانا یا ناسازگار با هدر همین ردیف
    /// (مالک/شناسه/بازبینی/نسخهٔ قالب) خطای پایدار می‌دهد و هرگز مقدار «تهی» برنمی‌گرداند.
    /// </summary>
    public ShippingAuthoringRevisionPayload ReadPayload()
    {
        var payload = ShippingAuthoringRevisionSnapshotCodec.Deserialize(Payload);

        // The ROW headers are the authority: a body that describes another owner/draft/revision/format is a
        // corrupt row, not a read to be silently trusted.
        if (payload.DraftId != DraftId
            || payload.SourceRevision != SourceRevision
            || payload.FormatVersion != FormatVersion
            || payload.Owner.Kind != OwnerKind
            || !string.Equals(payload.Owner.TenantId, OwnerTenantId, StringComparison.Ordinal)
            || payload.Owner.SellerPartyId != OwnerSellerPartyId)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRevisionSnapshotInvalid));
        }

        return payload;
    }

    /// <summary>
    /// یک بدنهٔ ذخیره‌شده را برای یک خوانندهٔ آیندهٔ همین ماژول رمزگشایی می‌کند؛ نسخهٔ ناشناخته یا
    /// بدنهٔ ناخوانا/ناقص خطای پایدار می‌دهد و هرگز مقدار «تهی» برنمی‌گرداند.
    /// </summary>
    /// <param name="payload">متن JSONB ذخیره‌شده.</param>
    public static ShippingAuthoringRevisionPayload DecodeStoredPayload(string payload)
        => ShippingAuthoringRevisionSnapshotCodec.Deserialize(payload);
}

/// <summary>
/// رمزگذار صریح و تایپ‌شدهٔ بدنهٔ capture؛ تنها مرز تبدیل بین aggregate و JSONB.
/// </summary>
/// <remarks>
/// هیچ بدنهٔ HTTP، نام نوع، دیکشنری عمومی یا بازتاب نوع در اینجا ذخیره نمی‌شود. نسخهٔ قالب بخشی از
/// payload است و نسخهٔ ناشناخته رد می‌شود؛ رمزگشایی ناموفق هرگز «تهی» برنمی‌گرداند.
/// </remarks>
internal static class ShippingAuthoringRevisionSnapshotCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // Decimal/TimeOnly fraction fidelity matters: no custom rounding or truncation is introduced.
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>کل گراف ذخیره‌شده را به بدنهٔ تایپ‌شده تبدیل می‌کند (بدون خواندن منبع بیرونی).</summary>
    /// <param name="draft">پیش‌نویس با گراف کامل.</param>
    internal static ShippingAuthoringRevisionPayload FromDraft(ShippingAuthoringDraft draft)
    {
        var groups = draft.RegionGroups
            .OrderBy(group => group.Order)
            .Select(group => new ShippingAuthoringRevisionGroup(
                group.GroupId,
                group.Order,
                group.CurrencyCode,
                group.Areas
                    .OrderBy(area => area.SelectedAreaId)
                    .Select(area => new ShippingAuthoringRevisionArea(
                        area.SelectedAreaId,
                        area.NodeKind,
                        area.NodeId))
                    .ToArray(),
                group.RateRows
                    .OrderBy(row => row.RateRowId)
                    .Select(row => new ShippingAuthoringRevisionRateRow(
                        row.RateRowId,
                        row.MinWeightGrams,
                        row.MaxWeightGrams,
                        row.MinItemCount,
                        row.MaxItemCount,
                        row.MinMerchandiseAmount,
                        row.MaxMerchandiseAmount,
                        row.FreightAmount,
                        row.PackagingAmount,
                        row.MinDeliveryDays,
                        row.MaxDeliveryDays))
                    .ToArray(),
                group.Titles
                    .OrderBy(title => title.LanguageId)
                    .Select(title => new ShippingAuthoringRevisionGroupTitle(title.LanguageId, title.Title))
                    .ToArray()))
            .ToArray();

        var settings = draft.MethodSettings is { } methodSettings
            ? new ShippingAuthoringRevisionSettings(
                methodSettings.FreightPaymentMode,
                methodSettings.CalculationMode,
                methodSettings.UpdatedAt)
            : null;

        var week = draft.WeeklySchedule is { } schedule
            ? new ShippingAuthoringRevisionWeek(
                schedule.TimeZoneId,
                schedule.UpdatedAt,
                schedule.Days
                    .OrderBy(day => day.Weekday)
                    .Select(day => new ShippingAuthoringRevisionWeekDay(
                        day.Weekday,
                        day.IsWorking,
                        day.StartTime,
                        day.EndTime))
                    .ToArray(),
                schedule.SlotDurationMinutes)
            : null;

        var formatVersion = week?.SlotDurationMinutes is null
            ? ShippingAuthoringRevisionSnapshot.LegacyFormatVersion
            : ShippingAuthoringRevisionSnapshot.SlotDurationFormatVersion;

        return new ShippingAuthoringRevisionPayload(
            formatVersion,
            new ShippingAuthoringRevisionOwner(
                draft.OwnerKind,
                draft.OwnerTenantId,
                draft.OwnerSellerPartyId),
            draft.ServiceCode,
            draft.OptionCode,
            draft.DraftId,
            draft.Revision,
            draft.CreatedAt,
            draft.UpdatedAt,
            draft.ShippingTypeId,
            groups,
            draft.Translations
                .OrderBy(translation => translation.LanguageId)
                .Select(translation => new ShippingAuthoringRevisionTranslation(
                    translation.LanguageId,
                    translation.Name,
                    translation.Description))
                .ToArray(),
            settings,
            week);
    }

    /// <summary>بدنه را به JSON پایدار تبدیل می‌کند.</summary>
    /// <param name="payload">بدنهٔ تایپ‌شده.</param>
    internal static string Serialize(ShippingAuthoringRevisionPayload payload)
        => JsonSerializer.Serialize(payload, Options);

    /// <summary>بدنهٔ ذخیره‌شده را رمزگشایی می‌کند؛ نسخهٔ ناشناخته/بدنهٔ ناخوانا رد می‌شود.</summary>
    /// <param name="json">متن ذخیره‌شده.</param>
    internal static ShippingAuthoringRevisionPayload Deserialize(string json)
    {
        ShippingAuthoringRevisionPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ShippingAuthoringRevisionPayload>(json, Options);
        }
        catch (JsonException)
        {
            throw new SemanticException(
                new SemanticError(ShippingErrorCodes.AuthoringRevisionSnapshotInvalid));
        }

        if (payload is null
            || !ShippingAuthoringRevisionSnapshot.IsSupportedFormatVersion(payload.FormatVersion)
            || payload.Groups is null
            || payload.Translations is null
            || payload.Owner is null)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRevisionSnapshotInvalid));
        }

        ValidateShape(payload);
        return payload;
    }

    /// <summary>
    /// شکل الزامی بدنه را می‌سنجد: هویت مالک سازگار، شناسه/بازبینی/کد خدمت واقعی، مجموعه‌های الزامی و
    /// اعضای تودرتوی الزامی. این اعتبارسنجی <b>قیمت‌گذاری</b> و کامل‌بودن پیکربندی نیست: یک پیش‌نویس
    /// خالی/ناقص، capture معتبر است.
    /// </summary>
    /// <param name="payload">بدنهٔ رمزگشایی‌شده.</param>
    private static void ValidateShape(ShippingAuthoringRevisionPayload payload)
    {
        var ownerCoherent = payload.Owner.Kind switch
        {
            ShippingDraftOwnerKind.Store =>
                !string.IsNullOrWhiteSpace(payload.Owner.TenantId) && payload.Owner.SellerPartyId is null,
            ShippingDraftOwnerKind.Marketplace =>
                payload.Owner.TenantId is null && payload.Owner.SellerPartyId is null,
            ShippingDraftOwnerKind.Seller =>
                payload.Owner.TenantId is null
                && payload.Owner.SellerPartyId is { } seller
                && seller != Guid.Empty,
            _ => false,
        };

        if (!ownerCoherent
            || payload.DraftId == Guid.Empty
            || payload.SourceRevision <= 0
            || string.IsNullOrWhiteSpace(payload.ServiceCode))
        {
            Reject();
        }

        foreach (var group in payload.Groups)
        {
            if (group is null
                || group.GroupId == Guid.Empty
                || group.Order < 0
                || group.Areas is null
                || group.RateRows is null
                || group.Titles is null)
            {
                Reject();
            }

            foreach (var area in group.Areas)
            {
                if (area is null
                    || area.SelectedAreaId == Guid.Empty
                    || area.NodeId == Guid.Empty
                    || !Enum.IsDefined(area.NodeKind))
                {
                    Reject();
                }
            }

            // Only required identity is enforced: an existing captured row's amounts/bounds are facts, and
            // no pricing/completeness rule is invented here.
            foreach (var row in group.RateRows)
            {
                if (row is null || row.RateRowId == Guid.Empty)
                {
                    Reject();
                }
            }

            foreach (var title in group.Titles)
            {
                if (title is null || title.LanguageId == Guid.Empty || title.Title is null)
                {
                    Reject();
                }
            }
        }

        foreach (var translation in payload.Translations)
        {
            if (translation is null || translation.LanguageId == Guid.Empty || translation.Name is null)
            {
                Reject();
            }
        }

        if (payload.MethodSettings is { } settings
            && (!Enum.IsDefined(settings.FreightPaymentMode) || !Enum.IsDefined(settings.CalculationMode)))
        {
            Reject();
        }

        if (payload.WeeklySchedule is { } week)
        {
            if (string.IsNullOrWhiteSpace(week.TimeZoneId) || week.Days is null)
            {
                Reject();
            }

            // TB-241: the slot duration is part of the versioned contract, not a free extra. A version-1
            // body must stay a full-day window and a version-2 body must carry a real 1..1440 duration.
            var durationValid = payload.FormatVersion == ShippingAuthoringRevisionSnapshot.LegacyFormatVersion
                ? week.SlotDurationMinutes is null
                : week.SlotDurationMinutes is >= 1 and <= 1440;
            if (!durationValid)
            {
                Reject();
            }

            foreach (var day in week.Days)
            {
                if (day is null || day.Weekday is < 1 or > 7)
                {
                    Reject();
                }
            }
        }
    }

    private static void Reject()
        => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRevisionSnapshotInvalid));
}
