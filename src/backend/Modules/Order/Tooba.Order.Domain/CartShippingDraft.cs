namespace Tooba.Order.Domain;

/// <summary>
/// پیش‌نویس ارسال فروشگاهی وابسته به سبد. حقیقت قیمت/حداقل تحویل از backend است نه React.
/// </summary>
public sealed class CartShippingDraft
{
    /// <summary>حداکثر طول یادداشت مشتری.</summary>
    public const int CustomerNoteMaxLength = 500;

    /// <summary>سازندهٔ EF.</summary>
    private CartShippingDraft()
    {
    }

    /// <summary>شناسهٔ سبد مالک.</summary>
    public Guid CartId { get; init; }

    /// <summary>هش راز مهمان؛ برای سبد احرازشده خالی است.</summary>
    public string GuestSecretHash { get; private set; } = string.Empty;

    /// <summary>نسخهٔ سبد هنگام ذخیره.</summary>
    public int CartVersion { get; private set; }

    /// <summary>نام گیرنده.</summary>
    public string RecipientName { get; private set; } = string.Empty;

    /// <summary>موبایل گیرنده.</summary>
    public string ContactMobile { get; private set; } = string.Empty;

    /// <summary>استان.</summary>
    public string ProvinceName { get; private set; } = string.Empty;

    /// <summary>شهر.</summary>
    public string CityName { get; private set; } = string.Empty;

    /// <summary>نشانی.</summary>
    public string PostalAddress { get; private set; } = string.Empty;

    /// <summary>کد پستی.</summary>
    public string PostalCode { get; private set; } = string.Empty;

    /// <summary>شناسهٔ نشانی ذخیره‌شده (اختیاری؛ فقط مرجع انتخاب).</summary>
    public Guid? SavedAddressId { get; private set; }

    /// <summary>کد روش ارسال Store-enabled.</summary>
    public string ShippingMethodCode { get; private set; } = string.Empty;

    /// <summary>برچسب انسانی روش.</summary>
    public string ShippingMethodLabel { get; private set; } = string.Empty;

    /// <summary>مبلغ ارسال نقل‌قول‌شدهٔ backend.</summary>
    public decimal ShippingAmount { get; private set; }

    /// <summary>حداقل تاریخ تحویل محاسبه‌شده.</summary>
    public DateOnly MinimumDeliveryDate { get; private set; }

    /// <summary>تاریخ تحویل انتخابی مشتری.</summary>
    public DateOnly? SelectedDeliveryDate { get; private set; }

    /// <summary>پنجرهٔ ساعتی انتخابی.</summary>
    public string? SelectedDeliveryTimeWindow { get; private set; }

    /// <summary>یادداشت مشتری برای ارسال (نه یادداشت Admin).</summary>
    public string? CustomerNote { get; private set; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>پیش‌نویس جدید یا جایگزین می‌سازد.</summary>
    public static CartShippingDraft Create(
        Guid cartId,
        string? guestSecretHash,
        int cartVersion,
        string recipientName,
        string contactMobile,
        string provinceName,
        string cityName,
        string postalAddress,
        string postalCode,
        Guid? savedAddressId,
        string shippingMethodCode,
        string shippingMethodLabel,
        decimal shippingAmount,
        DateOnly minimumDeliveryDate,
        DateOnly? selectedDeliveryDate,
        string? selectedDeliveryTimeWindow,
        string? customerNote,
        DateTimeOffset now)
    {
        var draft = new CartShippingDraft { CartId = cartId };
        draft.Replace(
            guestSecretHash,
            cartVersion,
            recipientName,
            contactMobile,
            provinceName,
            cityName,
            postalAddress,
            postalCode,
            savedAddressId,
            shippingMethodCode,
            shippingMethodLabel,
            shippingAmount,
            minimumDeliveryDate,
            selectedDeliveryDate,
            selectedDeliveryTimeWindow,
            customerNote,
            now);
        return draft;
    }

    /// <summary>محتوای پیش‌نویس را جایگزین می‌کند.</summary>
    public void Replace(
        string? guestSecretHash,
        int cartVersion,
        string recipientName,
        string contactMobile,
        string provinceName,
        string cityName,
        string postalAddress,
        string postalCode,
        Guid? savedAddressId,
        string shippingMethodCode,
        string shippingMethodLabel,
        decimal shippingAmount,
        DateOnly minimumDeliveryDate,
        DateOnly? selectedDeliveryDate,
        string? selectedDeliveryTimeWindow,
        string? customerNote,
        DateTimeOffset now)
    {
        GuestSecretHash = string.IsNullOrWhiteSpace(guestSecretHash) ? string.Empty : guestSecretHash.Trim();
        CartVersion = cartVersion;
        RecipientName = recipientName.Trim();
        ContactMobile = contactMobile.Trim();
        ProvinceName = provinceName.Trim();
        CityName = cityName.Trim();
        PostalAddress = postalAddress.Trim();
        PostalCode = postalCode.Trim();
        SavedAddressId = savedAddressId is Guid id && id != Guid.Empty ? id : null;
        ShippingMethodCode = shippingMethodCode.Trim().ToLowerInvariant();
        ShippingMethodLabel = shippingMethodLabel.Trim();
        ShippingAmount = Math.Max(0m, shippingAmount);
        MinimumDeliveryDate = minimumDeliveryDate;
        SelectedDeliveryDate = selectedDeliveryDate;
        SelectedDeliveryTimeWindow = string.IsNullOrWhiteSpace(selectedDeliveryTimeWindow)
            ? null
            : selectedDeliveryTimeWindow.Trim();
        var note = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim();
        if (note is { Length: > CustomerNoteMaxLength })
        {
            throw new InvalidOperationException("shipping.note.too_long");
        }

        CustomerNote = note;
        UpdatedAt = now;
    }
}
