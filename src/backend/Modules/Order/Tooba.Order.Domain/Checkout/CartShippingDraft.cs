namespace Tooba.Order.Domain.Checkout;

using Tooba.BuildingBlocks;

/// <summary>
/// پیش‌نویس ارسال فروشگاهی وابسته به سبد. حقیقت قیمت/حداقل تحویل از backend است نه React.
/// علاوه بر فیلدهای تک‌روشی سازگار با گذشته، مالک مجموعهٔ گروه‌های ارسال چندگانهٔ همین سبد است.
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

    /// <summary>نام کوچک گیرنده؛ رکورد قدیمی ممکن است خالی باشد.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>نام خانوادگی گیرنده؛ رکورد قدیمی ممکن است خالی باشد.</summary>
    public string LastName { get; private set; } = string.Empty;

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

    /// <summary>
    /// گروه‌های ارسال پیش‌نویس همین سبد. یک گروه = یک فروشنده + یک روش متعارف.
    /// برای پیش‌نویس‌های تک‌روشی قدیمی نیز همیشه حداقل یک گروه ساخته می‌شود.
    /// </summary>
    public List<CartShippingDraftGroup> ShippingGroups { get; } = [];

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
            throw new ContractOperationException("shipping.note.too_long");
        }

        CustomerNote = note;
        UpdatedAt = now;
    }

    /// <summary>پس از ورود، راز مهمان را از پیش‌نویس سبد احرازشده برمی‌دارد.</summary>
    public void ClearGuestSecret()
    {
        GuestSecretHash = string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>نام و نام خانوادگی را جداگانه نگه می‌دارد و نام نمایشی را فقط وقتی هر دو موجودند می‌سازد.</summary>
    public void ApplyRecipientNames(string? firstName, string? lastName)
    {
        FirstName = string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim();
        if (FirstName.Length > 0 && LastName.Length > 0)
        {
            RecipientName = $"{FirstName} {LastName}";
        }
    }

    /// <summary>
    /// مجموعهٔ گروه‌های ارسال پیش‌نویس را پاک می‌کند (فاز نخست جایگزینی، تا حذف‌ها پیش از درج
    /// دوبارهٔ همان cart_line_id روی ایندکس یگانه اجرا شوند).
    /// </summary>
    public void ClearShippingGroups(DateTimeOffset now)
    {
        if (ShippingGroups.Count == 0)
        {
            return;
        }

        ShippingGroups.Clear();
        UpdatedAt = now;
    }

    /// <summary>
    /// گروه‌های ارسال پیش‌نویس را جایگزین می‌کند (فاز دوم؛ پس از flush حذف‌ها).
    /// فیلدهای تک‌روشی سازگار با گذشته به‌صورت projection از همین گروه‌ها به‌روز می‌شوند تا
    /// خواننده‌های قدیمی (پیش‌نویس پرداخت/Commit) بدون تغییر کار کنند.
    /// </summary>
    public void AddShippingGroups(IReadOnlyList<CartShippingDraftGroup> groups, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.GroupWithoutLines);
        }

        ShippingGroups.AddRange(groups);

        // سازگاری گذشته: فیلد تک‌روشی فقط projection است، نه حقیقت.
        var first = groups[0];
        ShippingMethodCode = first.MethodCode;
        ShippingMethodLabel = first.MethodLabel;
        ShippingAmount = groups.Sum(x => x.ShippingAmount);
        MinimumDeliveryDate = first.MinimumDeliveryDate;
        SelectedDeliveryDate = first.SelectedDeliveryDate;
        SelectedDeliveryTimeWindow = first.SelectedDeliveryTimeWindow;
        UpdatedAt = now;
    }
}
