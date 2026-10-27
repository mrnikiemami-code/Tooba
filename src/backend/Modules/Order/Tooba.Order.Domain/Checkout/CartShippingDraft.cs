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
    /// نسخهٔ یکنوای صعودیِ انتخاب (TB-232): هر جایگزینی موفق انتخاب دقیقاً یک واحد بالا می‌رود و
    /// نوشتن جدید فقط با نسخهٔ موردانتظار برابر مجاز است. هرگز بازنشانی نمی‌شود (بدون ABA) و
    /// هیچ‌گاه از کلاینت به‌عنوان حقیقت پذیرفته نمی‌شود.
    /// </summary>
    public long SelectionRevision { get; private set; }

    /// <summary>شناسهٔ پایدار کشورِ مقصدی که قیمت با آن محاسبه شد (TB-232).</summary>
    public Guid? DestinationCountryId { get; private set; }

    /// <summary>شناسهٔ پایدار ناحیهٔ اداریِ همین مقصد یا تهی.</summary>
    public Guid? DestinationAdministrativeAreaId { get; private set; }

    /// <summary>شناسهٔ پایدار شهرِ همین مقصد یا تهی.</summary>
    public Guid? DestinationCityId { get; private set; }

    /// <summary>شناسهٔ پایدار ناحیهٔ خدمتِ همین مقصد یا تهی.</summary>
    public Guid? DestinationServiceAreaId { get; private set; }

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

    /// <summary>
    /// نسخهٔ انتخاب را یک واحد بالا می‌برد. فقط نویسندهٔ اتمیک سرور پس از تأیید CAS صدا می‌زند؛
    /// هیچ transport این متد را مستقیم اجرا نمی‌کند.
    /// </summary>
    public void BumpSelectionRevision(DateTimeOffset now)
    {
        SelectionRevision = SelectionRevision == long.MaxValue
            ? throw new ContractOperationException("shipping.selection.revision.overflow")
            : SelectionRevision + 1;
        UpdatedAt = now;
    }

    /// <summary>
    /// هویت مقصد تأییدشدهٔ همان قیمت را ذخیره می‌کند (چهار اسکالر، بدون JSON جغرافیایی). فقط پس از
    /// تأیید یکتای Geography صدا زده می‌شود؛ هیچ مقداری از مرورگر بدون تأیید این‌جا نمی‌نشیند.
    /// </summary>
    public void SetDestinationIdentity(
        Guid countryId,
        Guid? administrativeAreaId,
        Guid? cityId,
        Guid? serviceAreaId,
        DateTimeOffset now)
    {
        if (countryId == Guid.Empty)
        {
            throw new ContractOperationException("shipping.destination.country.required");
        }

        DestinationCountryId = countryId;
        DestinationAdministrativeAreaId = administrativeAreaId is Guid area && area != Guid.Empty ? area : null;
        DestinationCityId = cityId is Guid city && city != Guid.Empty ? city : null;
        DestinationServiceAreaId = serviceAreaId is Guid service && service != Guid.Empty ? service : null;
        UpdatedAt = now;
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

        // سازگاری گذشته: فیلد تک‌روشی فقط projection است، نه حقیقت. اگر پیش‌نویس بیش از یک گروه
        // داشته باشد هیچ روش واحدی بر همهٔ خطوط اعمال نمی‌شود، پس ستون‌های تک‌روشی خالی می‌مانند.
        var first = groups[0];
        var multiGroup = groups.Count > 1;
        ShippingMethodCode = multiGroup ? string.Empty : first.MethodCode;
        ShippingMethodLabel = multiGroup ? string.Empty : first.MethodLabel;
        ShippingAmount = groups.Sum(x => x.ShippingAmount);
        // TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: در سبد چندگروهی هیچ تاریخ/پنجرهٔ سراسریِ
        // authoritative وجود ندارد؛ انتخاب‌شده‌ها Null می‌شوند تا اسکالر قدیمی به‌غلط حقیقت گروه‌ها
        // جا نزند. حداقل تاریخ (ستون غیرnullable قدیمی) فقط یک کران محافظه‌کارانهٔ دیرترین گروه است.
        SelectedDeliveryDate = multiGroup ? null : first.SelectedDeliveryDate;
        SelectedDeliveryTimeWindow = multiGroup ? null : first.SelectedDeliveryTimeWindow;
        MinimumDeliveryDate = multiGroup
            ? groups.Max(x => x.MinimumDeliveryDate)
            : first.MinimumDeliveryDate;
        UpdatedAt = now;
    }
}
