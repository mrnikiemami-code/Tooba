using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// مقادیر متعارف یک ردیف نرخ دستی، پس از عبور از all-or-nothing پیش‌سنجی.
/// </summary>
/// <remarks>
/// <para>
/// <b>سه محور شرط، همه نیم‌باز <c>[min, max)</c>.</b> مقدار تهی در یک محور یعنی «این ردیف روی آن
/// محور شرط ندارد»؛ مقدار داشتن یعنی «فقط داخل این بازه». کران پایین همیشه مشخص است و کران بالا
/// می‌تواند باز باشد، دقیقاً مانند <see cref="Tooba.Shipping.Contracts.Rates.ShippingNumericTier"/>.
/// </para>
/// <para>
/// <b>شرط‌های یک ردیف AND، ردیف‌ها جایگزین.</b> دو ردیف فقط وقتی مبهم‌اند که حاصل‌ضرب کامل سه محورشان
/// یک نقطهٔ معتبر مشترک داشته باشد. بازه‌های چسبیده هم‌پوشانی ندارند.
/// </para>
/// </remarks>
/// <param name="MinWeightGrams">کران پایین وزن (شامل)؛ تهی یعنی بی‌شرط.</param>
/// <param name="MaxWeightGrams">کران بالای وزن (غیرشامل).</param>
/// <param name="MinItemCount">کران پایین تعداد واحد کالا (شامل)؛ تهی یعنی بی‌شرط.</param>
/// <param name="MaxItemCount">کران بالای تعداد واحد (غیرشامل).</param>
/// <param name="MinMerchandiseAmount">کران پایین مبلغ کالای گروه (شامل)؛ تهی یعنی بی‌شرط.</param>
/// <param name="MaxMerchandiseAmount">کران بالای مبلغ کالای گروه (غیرشامل).</param>
/// <param name="FreightAmount">هزینهٔ حمل.</param>
/// <param name="PackagingAmount">هزینهٔ بسته‌بندی، مستقل از حمل.</param>
/// <param name="MinDeliveryDays">کمینهٔ مدت تحویل (روز).</param>
/// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل (روز)؛ مساوی مجاز است.</param>
public sealed record ShippingAuthoringRateRowValue(
    long? MinWeightGrams,
    long? MaxWeightGrams,
    int? MinItemCount,
    int? MaxItemCount,
    decimal? MinMerchandiseAmount,
    decimal? MaxMerchandiseAmount,
    decimal FreightAmount,
    decimal PackagingAmount,
    int MinDeliveryDays,
    int MaxDeliveryDays)
{
    /// <summary>آیا این ردیف روی محور وزن شرط دارد.</summary>
    public bool IsWeightConditioned => MinWeightGrams is not null;

    /// <summary>آیا این ردیف روی محور تعداد شرط دارد.</summary>
    public bool IsCountConditioned => MinItemCount is not null;

    /// <summary>آیا این ردیف روی محور مبلغ کالا شرط دارد.</summary>
    public bool IsAmountConditioned => MinMerchandiseAmount is not null;

    /// <summary>
    /// آیا دو ردیف در یک گروه هم‌پوشانی دارند. هر محورِ بی‌شرط در کل دامنهٔ معتبر همان محور نامحدود
    /// فرض می‌شود، پس یک ردیف بی‌شرط با هر ردیف دیگری در همان گروه تعارض دارد.
    /// </summary>
    /// <param name="other">ردیف دیگر.</param>
    public bool Overlaps(ShippingAuthoringRateRowValue other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return RangesOverlap(MinWeightGrams, MaxWeightGrams, other.MinWeightGrams, other.MaxWeightGrams)
            && RangesOverlap(MinItemCount, MaxItemCount, other.MinItemCount, other.MaxItemCount)
            && AmountsOverlap(
                MinMerchandiseAmount,
                MaxMerchandiseAmount,
                other.MinMerchandiseAmount,
                other.MaxMerchandiseAmount);
    }

    /// <summary>
    /// هم‌پوشانی دو بازهٔ نیم‌باز عدد صحیح؛ تهی یعنی نامحدود در آن سمت. کران‌های چسبیده
    /// (<c>[1000,2000)</c> و <c>[2000,3000)</c>) هم‌پوشانی ندارند.
    /// </summary>
    private static bool RangesOverlap(long? min, long? max, long? otherMin, long? otherMax)
    {
        var lower = min ?? long.MinValue;
        var otherLower = otherMin ?? long.MinValue;
        var upper = max;
        var otherUpper = otherMax;
        return (upper is not { } end || otherLower < end)
            && (otherUpper is not { } otherEnd || lower < otherEnd);
    }

    /// <summary>هم‌پوشانی دو بازهٔ مبلغ؛ همان معنای نیم‌باز و همان قاعدهٔ کران‌های چسبیده.</summary>
    private static bool AmountsOverlap(decimal? min, decimal? max, decimal? otherMin, decimal? otherMax)
    {
        var lower = min ?? decimal.MinValue;
        var otherLower = otherMin ?? decimal.MinValue;
        return (max is not { } end || otherLower < end)
            && (otherMax is not { } otherEnd || lower < otherEnd);
    }
}

/// <summary>
/// یک ردیف نرخ دستی ماندگار زیر یک گروه منطقه‌ای. این ردیف <b>هیچ جغرافیایی</b> ندارد: تنها ارجاع
/// مکانی همان <see cref="GroupId"/> پایدار است، پس جغرافیا یک‌بار روی گروه ذخیره می‌شود.
/// </summary>
/// <remarks>
/// این slice فقط ذخیرهٔ پیکربندی آستانه است. هیچ ارزیاب سبد/مرسوله، قیمت مشتری، فعال‌سازی یا
/// زمان‌بندی از این ردیف خوانده نمی‌شود، و مبنای authoritative اندازه/مبلغ کالا همچنان یک دروازهٔ
/// یکپارچه‌سازی باز است.
/// </remarks>
public sealed class ShippingAuthoringRateRow
{
    private ShippingAuthoringRateRow()
    {
    }

    /// <summary>سقف فنی تعداد ردیف در یک گروه (محدودیت payload، نه سقف تجاری).</summary>
    public const int MaxRowsPerGroup = 200;

    /// <summary>شناسهٔ پایدار ردیف؛ سرورساخته و در همان گروه قابل حفظ است.</summary>
    public Guid RateRowId { get; private set; }

    /// <summary>گروه مالک؛ تنها ارجاع مکانی این ردیف.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>کران پایین وزن (گرم، شامل).</summary>
    public long? MinWeightGrams { get; private set; }

    /// <summary>کران بالای وزن (گرم، غیرشامل).</summary>
    public long? MaxWeightGrams { get; private set; }

    /// <summary>کران پایین تعداد واحد کالا (شامل).</summary>
    public int? MinItemCount { get; private set; }

    /// <summary>کران بالای تعداد واحد کالا (غیرشامل).</summary>
    public int? MaxItemCount { get; private set; }

    /// <summary>کران پایین مبلغ کالای گروه (شامل).</summary>
    public decimal? MinMerchandiseAmount { get; private set; }

    /// <summary>کران بالای مبلغ کالای گروه (غیرشامل).</summary>
    public decimal? MaxMerchandiseAmount { get; private set; }

    /// <summary>هزینهٔ حمل.</summary>
    public decimal FreightAmount { get; private set; }

    /// <summary>هزینهٔ بسته‌بندی.</summary>
    public decimal PackagingAmount { get; private set; }

    /// <summary>کمینهٔ مدت تحویل بر حسب روز.</summary>
    public int MinDeliveryDays { get; private set; }

    /// <summary>بیشینهٔ مدت تحویل بر حسب روز.</summary>
    public int MaxDeliveryDays { get; private set; }

    /// <summary>مقادیر متعارف این ردیف.</summary>
    public ShippingAuthoringRateRowValue Value => new(
        MinWeightGrams,
        MaxWeightGrams,
        MinItemCount,
        MaxItemCount,
        MinMerchandiseAmount,
        MaxMerchandiseAmount,
        FreightAmount,
        PackagingAmount,
        MinDeliveryDays,
        MaxDeliveryDays);

    internal static ShippingAuthoringRateRow Create(Guid groupId, ShippingAuthoringRateRowValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var row = new ShippingAuthoringRateRow
        {
            RateRowId = UuidV7.New(),
            GroupId = groupId,
        };
        row.Apply(value);
        return row;
    }

    /// <summary>همان هویت ردیف را با مقادیر تازه نگه می‌دارد؛ شناسه و گروه تغییر نمی‌کنند.</summary>
    internal void Apply(ShippingAuthoringRateRowValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        MinWeightGrams = value.MinWeightGrams;
        MaxWeightGrams = value.MaxWeightGrams;
        MinItemCount = value.MinItemCount;
        MaxItemCount = value.MaxItemCount;
        MinMerchandiseAmount = value.MinMerchandiseAmount;
        MaxMerchandiseAmount = value.MaxMerchandiseAmount;
        FreightAmount = value.FreightAmount;
        PackagingAmount = value.PackagingAmount;
        MinDeliveryDays = value.MinDeliveryDays;
        MaxDeliveryDays = value.MaxDeliveryDays;
    }
}

/// <summary>
/// قواعد متعارف شکل/محدودهٔ یک ردیف نرخ، مشترک بین پیش‌سنجی دامنه و اسکنرهای ساختاری.
/// </summary>
/// <remarks>
/// سقف‌ها از ثابت‌های canonical سیاست نرخ موجود گرفته می‌شوند تا دو مدل عددی موازی ساخته نشود.
/// </remarks>
public static class ShippingAuthoringRateRowRules
{
    /// <summary>سقف وزن (گرم) هم‌تراز با سیاست نرخ canonical.</summary>
    public const long WeightGramsMaxValue = ShippingRatePolicy.WeightGramsMaxValue;

    /// <summary>سقف تعداد واحد کالا هم‌تراز با سیاست نرخ canonical.</summary>
    public const int ItemCountMaxValue = ShippingRatePolicy.ItemCountMaxValue;

    /// <summary>سقف مبلغ هم‌تراز با سیاست نرخ canonical.</summary>
    public const decimal AmountMaxValue = ShippingRatePolicy.ChargeAmountMaxValue;

    /// <summary>سقف روز تحویل هم‌تراز با سیاست نرخ canonical.</summary>
    public const int DaysMaxValue = ShippingRatePolicy.DaysMaxValue;

    /// <summary>تعداد رقم اعشار قابل‌نمایش در ستون <c>numeric(18,2)</c>.</summary>
    public const int AmountScale = 2;

    /// <summary>آیا مبلغ دقیقاً در دقت ذخیره‌سازی canonical قابل نمایش است.</summary>
    /// <param name="amount">مبلغ.</param>
    public static bool IsRepresentableAmount(decimal amount)
        => decimal.Round(amount, AmountScale, MidpointRounding.ToZero) == amount;

    /// <summary>خطای شکل/دامنهٔ یک ردیف را پرتاب می‌کند؛ پیام هرگز به کلاینت نمی‌رود.</summary>
    /// <param name="code">کد پایدار.</param>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    internal static void Reject(string code)
        => throw new SemanticException(new SemanticError(code));
}
