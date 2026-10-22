using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Scheduling;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// یک گروه منطقه‌ای پیشنهادی برای جایگزینی درخت پیش‌نویس. شناسهٔ گروه اختیاری است: تهی یعنی گروه
/// تازه، و شناسهٔ موجود یعنی همان گروه با هویت پایدارش دوباره استفاده شود.
/// </summary>
/// <param name="GroupId">شناسهٔ گروه موجود همین پیش‌نویس؛ تهی برای گروه تازه.</param>
/// <param name="Selections">انتخاب‌های اثبات‌شدهٔ زیر این گروه.</param>
public sealed record ShippingAuthoringRegionGroupInput(
    Guid? GroupId,
    IReadOnlyList<ShippingDestinationNode> Selections);

/// <summary>
/// یک انتخاب جغرافیایی ذخیره‌شده زیر یک گروه. ردیف، هویت خودش را دارد اما هویت <em>تجاری</em> همان
/// (گونه، شناسهٔ گره) است.
/// </summary>
public sealed class ShippingAuthoringSelectedArea
{
    private ShippingAuthoringSelectedArea()
    {
    }

    /// <summary>شناسهٔ ردیف (پایدار روی خواندن/جایگزینی).</summary>
    public Guid SelectedAreaId { get; private set; }

    /// <summary>
    /// شناسهٔ پیش‌نویس مالک این ردیف. عمداً یک ستون denormalized است تا یکتایی «یک انتخاب در کل
    /// پیش‌نویس» در سطح دیتابیس قابل تضمین باشد؛ هیچ FK دومی به drafts ساخته نمی‌شود.
    /// </summary>
    public Guid DraftId { get; private set; }

    /// <summary>گروه مالک این انتخاب.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>گونهٔ گرهٔ انتخاب‌شده.</summary>
    public ShippingDestinationNodeKind NodeKind { get; private set; }

    /// <summary>شناسهٔ پایدار گرهٔ انتخاب‌شده.</summary>
    public Guid NodeId { get; private set; }

    /// <summary>گرهٔ این انتخاب.</summary>
    public ShippingDestinationNode Node => new(NodeKind, NodeId);

    /// <summary>
    /// گروه مالک را بدون تغییر هویت ردیف جابه‌جا می‌کند. انتقال یک انتخاب بین گروه‌ها یک UPDATE است،
    /// نه حذف+درج؛ پس یکتایی <c>(draft_id, node_kind, node_id)</c> در هیچ لحظه‌ای نقض نمی‌شود.
    /// </summary>
    /// <param name="groupId">گروه مقصد که به همین پیش‌نویس تعلق دارد.</param>
    internal void Retarget(Guid groupId)
    {
        if (groupId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
        }

        GroupId = groupId;
    }

    internal static ShippingAuthoringSelectedArea Create(Guid draftId, Guid groupId, ShippingDestinationNode node)
    {
        if (!node.IsValid)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
        }

        return new ShippingAuthoringSelectedArea
        {
            SelectedAreaId = UuidV7.New(),
            DraftId = draftId,
            GroupId = groupId,
            NodeKind = node.Kind,
            NodeId = node.Id,
        };
    }
}

/// <summary>
/// یک گروه منطقه‌ای با هویت پایدار و ترتیب صریح. جغرافیا <b>یک‌بار</b> روی گروه ذخیره می‌شود و
/// ردیف‌های آیندهٔ نرخ/بسته‌بندی/زمان‌بندی فقط به همین شناسهٔ گروه ارجاع می‌دهند.
/// </summary>
public sealed class ShippingAuthoringRegionGroup
{
    private readonly List<ShippingAuthoringSelectedArea> _areas = [];
    private readonly List<ShippingAuthoringRateRow> _rateRows = [];
    private readonly List<ShippingAuthoringRegionGroupTitle> _titles = [];

    private ShippingAuthoringRegionGroup()
    {
    }

    /// <summary>شناسهٔ پایدار گروه؛ مرجع ردیف‌های نرخ/بسته‌بندی/زمان‌بندی.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>شناسهٔ پیش‌نویس مالک (ستون denormalized همان قاعدهٔ یکتایی).</summary>
    public Guid DraftId { get; private set; }

    /// <summary>ترتیب صریح گروه در پیش‌نویس (صفرپایه).</summary>
    public int Order { get; private set; }

    /// <summary>
    /// ارز صریح مجموعهٔ ردیف‌های نرخ این گروه؛ تهی یعنی هنوز هیچ ردیفی ذخیره نشده است. پس از نخستین
    /// ذخیرهٔ مجموعهٔ غیرخالی، این مقدار bind می‌شود و با پاک‌کردن ردیف‌ها هم پاک نمی‌شود تا مبالغ
    /// قدیمی هرگز بی‌سروصدا با ارز دیگری بازتفسیر نشوند.
    /// </summary>
    public string? CurrencyCode { get; private set; }

    /// <summary>انتخاب‌های این گروه، مرتب‌شده برای خواندن قطعی.</summary>
    public IReadOnlyList<ShippingAuthoringSelectedArea> Areas => _areas;

    /// <summary>ردیف‌های نرخ دستی این گروه؛ خالی یعنی پیکربندی ناقص، نه پوشش آزاد یا رایگان.</summary>
    public IReadOnlyList<ShippingAuthoringRateRow> RateRows => _rateRows;

    /// <summary>
    /// عنوان‌های اختیاری همین گروه به‌ازای هر زبان محتوا. عنوان هیچ اثری روی جغرافیا، ردیف‌ها یا
    /// ارز ندارد و حذف گروه فقط عنوان‌های خودش را cascade می‌کند.
    /// </summary>
    public IReadOnlyList<ShippingAuthoringRegionGroupTitle> Titles => _titles;

    internal static ShippingAuthoringRegionGroup Create(Guid draftId, Guid groupId, int order)
        => new()
        {
            GroupId = groupId,
            DraftId = draftId,
            Order = order,
        };

    /// <summary>
    /// ترتیب و انتخاب‌های این گروه را جایگزین می‌کند. هویت ردیف‌های موجود حفظ می‌شود تا جایگزینی یک
    /// درخت، حذف/درج یکتایی‌شکن در همان SaveChanges نسازد.
    /// </summary>
    /// <param name="order">ترتیب جدید (پایانی یا موقت در مرحلهٔ اول).</param>
    /// <param name="areas">انتخاب‌های جدید با هویت ردیف پایدار.</param>
    internal void ReplaceAreas(int order, IReadOnlyList<ShippingAuthoringSelectedArea> areas)
    {
        Order = order;
        _areas.Clear();
        _areas.AddRange(areas);
    }

    /// <summary>ترتیب صریح را بدون دست‌زدن به انتخاب‌ها می‌نشاند.</summary>
    /// <param name="order">ترتیب جدید.</param>
    internal void SetOrder(int order) => Order = order;

    /// <summary>
    /// انتخاب‌های این گروه را از فهرست جدا می‌کند و برمی‌گرداند. برای بازچینش کل درخت لازم است:
    /// هر انتخاب دقیقاً یک گروه مالک دارد و جابه‌جایی بین گروه‌ها با همان ردیف انجام می‌شود.
    /// </summary>
    internal IReadOnlyList<ShippingAuthoringSelectedArea> DetachAreas()
    {
        var detached = _areas.ToArray();
        _areas.Clear();
        return detached;
    }

    /// <summary>مجموعهٔ ردیف‌های نرخ را جایگزین می‌کند؛ هویت ردیف‌های باقی‌مانده حفظ می‌شود.</summary>
    /// <param name="rows">ردیف‌های تازه (شامل موجودهای به‌روزشده و ردیف‌های تازه).</param>
    internal void ReplaceRateRows(IReadOnlyList<ShippingAuthoringRateRow> rows)
    {
        _rateRows.Clear();
        _rateRows.AddRange(rows);
    }

    /// <summary>ارز صریح مجموعهٔ ردیف‌ها را برای نخستین‌بار bind می‌کند.</summary>
    /// <param name="currencyCode">ارز معتبر سرورساخته.</param>
    internal void BindCurrency(string currencyCode) => CurrencyCode = currencyCode;

    /// <summary>
    /// عنوان یک زبان را می‌نشاند یا (با تهی) فقط همان جفت را پاک می‌کند. سایر زبان‌ها دست‌نخورده
    /// می‌مانند.
    /// </summary>
    /// <param name="languageId">شناسهٔ زبان محتوا.</param>
    /// <param name="title">عنوان تأییدشده یا تهی برای پاک‌کردن.</param>
    /// <returns>آیا وضعیت واقعاً تغییر کرد.</returns>
    internal bool SetTitle(Guid languageId, string? title)
    {
        var existing = _titles.SingleOrDefault(candidate => candidate.LanguageId == languageId);
        if (title is null)
        {
            return existing is not null && _titles.Remove(existing);
        }

        if (existing is null)
        {
            _titles.Add(ShippingAuthoringRegionGroupTitle.Create(DraftId, GroupId, languageId, title));
            return true;
        }

        if (string.Equals(existing.Title, title, StringComparison.Ordinal))
        {
            return false;
        }

        existing.Apply(title);
        return true;
    }
}

/// <summary>
/// پیش‌نویس نویسندگی Shipping برای یک Binding روش ارسال: هویت مالک، Binding تغییرناپذیر
/// (کد خدمت + گزینهٔ اختیاری)، شمارهٔ بازبینی یکنوا و درخت گروه‌های منطقه‌ای.
/// </summary>
/// <remarks>
/// <para>
/// <b>Draft، نه فعال‌سازی.</b> این aggregate فقط پیکربندی ناقص را نگه می‌دارد: هیچ نرخ، ارز،
/// زمان‌بندی، انتشار یا مصرف‌کنندهٔ استعلامی در این slice وجود ندارد و ذخیرهٔ یک پیش‌نویس هیچ
/// روش ارسالی را «فعال» نمی‌کند. پیش‌نویس/گروه خالی یعنی پیکربندی ناقص، هرگز یعنی پوشش نامحدود
/// یا ارسال رایگان.
/// </para>
/// <para>
/// <b>Binding تغییرناپذیر.</b> کد خدمت و گزینهٔ اختیاری پس از ساخت تغییر نمی‌کنند؛ تغییر آن‌ها یک
/// پیش‌نویس دیگر است، نه ویرایش این یکی. هیچ کاتالوگ روش دومی اینجا ساخته نمی‌شود: کدها فقط
/// کلید سازگاری هویت Fulfillment هستند.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringDraft
{
    /// <summary>حداکثر طول کد خدمت (هم‌تراز با سیاست نرخ موجود).</summary>
    public const int ServiceCodeMaxLength = 64;

    /// <summary>حداکثر طول کد گزینه.</summary>
    public const int OptionCodeMaxLength = 64;

    /// <summary>سقف فنی تعداد گروه در یک پیش‌نویس (محدودیت payload، نه سقف تجاری).</summary>
    public const int MaxRegionGroups = 50;

    /// <summary>سقف فنی تعداد انتخاب در یک گروه.</summary>
    public const int MaxSelectionsPerGroup = 200;

    /// <summary>سقف فنی تعداد انتخاب در یک پیش‌نویس.</summary>
    public const int MaxSelectionsPerDraft = 500;

    /// <summary>سقف فنی تعداد ورودی نام/توضیح در یک PATCH ارائه.</summary>
    public const int MaxMethodTranslations = 100;

    /// <summary>سقف فنی تعداد ورودی عنوان گروه در یک PATCH ارائه.</summary>
    public const int MaxGroupTitleEntries = 1000;

    private readonly List<ShippingAuthoringRegionGroup> _regionGroups = [];
    private readonly List<ShippingAuthoringDraftTranslation> _translations = [];

    private ShippingAuthoringDraft()
    {
    }

    /// <summary>شناسهٔ پایدار پیش‌نویس.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>مالک ذخیره‌شده (فیلتر دسترسی داده، نه مجوز).</summary>
    public ShippingDraftOwner Owner =>
        ShippingDraftOwner.Restore(OwnerKind, OwnerTenantId, OwnerSellerPartyId);

    /// <summary>گونهٔ مالک به‌صورت ستون ماندگاری.</summary>
    public ShippingDraftOwnerKind OwnerKind { get; private set; }

    /// <summary>شناسهٔ پایدار Tenant به‌صورت ستون ماندگاری؛ فقط برای مالک Store.</summary>
    public string? OwnerTenantId { get; private set; }

    /// <summary>SellerPartyId به‌صورت ستون ماندگاری؛ فقط برای مالک Seller.</summary>
    public Guid? OwnerSellerPartyId { get; private set; }

    /// <summary>کد خدمت Fulfillment؛ کلید سازگاری، نه کپی ردیف کاتالوگ.</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>گزینهٔ روش ارسال؛ تهی یعنی Binding در سطح خود خدمت.</summary>
    public string? OptionCode { get; private set; }

    /// <summary>شمارهٔ بازبینی یکنوا؛ همین ستون predicate واقعی همزمانی است.</summary>
    public long Revision { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>زمان آخرین جایگزینی UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>گروه‌های منطقه‌ای به ترتیب صریح.</summary>
    public IReadOnlyList<ShippingAuthoringRegionGroup> RegionGroups => _regionGroups;

    /// <summary>
    /// نام/توضیح مالک برای هر زبان محتوا. این مجموعه مستقل از درخت منطقه‌ای و ردیف‌های نرخ است، پس
    /// جایگزینی منطقه/ردیف هیچ ترجمه‌ای را حذف نمی‌کند؛ فقط حذف صریح یک گروه عنوان‌های خودش را
    /// cascade می‌کند.
    /// </summary>
    public IReadOnlyList<ShippingAuthoringDraftTranslation> Translations => _translations;

    /// <summary>
    /// برنامهٔ عملیاتی هفتگی همین روش؛ تهی یعنی صریحاً <b>پیکربندی‌نشده</b> و نه هفت روز کاری/تعطیل.
    /// یک‌بار به خود پیش‌نویس وصل است، پس همهٔ گروه‌های منطقه‌ای همان را به اشتراک می‌گذارند.
    /// </summary>
    public ShippingAuthoringWeeklySchedule? WeeklySchedule { get; private set; }

    /// <summary>
    /// تنظیمات روش همین پیش‌نویس: زمان پرداخت کرایه و حالت محاسبه، دو انتخاب مستقل روی ریشهٔ روش.
    /// تهی یعنی صریحاً <b>تنظیم‌نشده</b> و نه <c>Manual</c>/<c>Prepaid</c>.
    /// </summary>
    public ShippingAuthoringMethodSettings? MethodSettings { get; private set; }

    /// <summary>
    /// انتخاب اختیاری نوع ارسال برای کل این روش: <b>یک</b> ارجاع به تعریف مشترک Fulfillment روی ریشهٔ
    /// پیش‌نویس، نه برای هر گروه/ردیف/زبان/برنامه. تهی یعنی صریحاً <b>انتخاب‌نشده</b>.
    /// </summary>
    /// <remarks>
    /// این فقط یک شناسهٔ پایدار است: هیچ کد، متن چندزبانه، ترتیب، وضعیت فعال یا تعریف کاتالوگ در
    /// Shipping کپی نمی‌شود و هیچ FK/navigation میان‌ماژولی ساخته نمی‌شود. انتخاب یک نوع هیچ روشی را
    /// منتشر/فعال نمی‌کند و نرخ، پوشش، برنامه، ارز یا تعهد مشتری نمی‌سازد.
    /// </remarks>
    public Guid? ShippingTypeId { get; private set; }

    /// <summary>پیش‌نویس تازه می‌سازد؛ بازبینی نخست همیشه ۱ است.</summary>
    /// <param name="owner">مالک سرورساخته.</param>
    /// <param name="serviceCode">کد خدمت.</param>
    /// <param name="optionCode">گزینهٔ اختیاری.</param>
    /// <param name="regionGroups">درخت اولیه (می‌تواند خالی باشد).</param>
    /// <param name="now">زمان UTC.</param>
    /// <param name="draftId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    public static ShippingAuthoringDraft Create(
        ShippingDraftOwner owner,
        string serviceCode,
        string? optionCode,
        IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
        DateTimeOffset now,
        Guid? draftId = null)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(regionGroups);

        var draft = new ShippingAuthoringDraft
        {
            DraftId = draftId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            OwnerKind = owner.Kind,
            OwnerTenantId = owner.TenantId,
            OwnerSellerPartyId = owner.SellerPartyId,
            ServiceCode = NormalizeServiceCode(serviceCode),
            OptionCode = NormalizeOptionCode(optionCode),
            Revision = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        draft.SetInitialGroups(regionGroups);
        return draft;
    }

    /// <summary>
    /// کل درخت گروه‌های منطقه‌ای را با درخت داده‌شده جایگزین می‌کند و بازبینی را یک واحد بالا
    /// می‌برد. هویت مالک و Binding دست‌نخورده می‌ماند.
    /// </summary>
    /// <param name="regionGroups">درخت پیشنهادی.</param>
    /// <param name="now">زمان UTC.</param>
    public void ReplaceRegionGroups(IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups, DateTimeOffset now)
    {
        var plan = PreflightRegionGroups(regionGroups);
        ApplyRegionGroups(plan, staged: false, now);
    }

    /// <summary>
    /// ترتیب موقت مرحلهٔ اول جایگزینی. یکتایی <c>(draft_id, group_order)</c> فوری است، پس یک جابه‌جایی
    /// ترتیب معتبر (A@0,B@1 → B@0,A@1) هیچ UPDATE اول مجاز ندارد؛ مرحلهٔ اول همهٔ گروه‌های باقی‌مانده را
    /// به بازهٔ موقتِ بدون‌برخورد می‌برد و مرحلهٔ دوم ترتیب نهایی صفرپایه را می‌نشاند.
    /// </summary>
    public const int StagedOrderBase = 1_000_000;

    /// <summary>
    /// یک ورودی برنامهٔ جایگزینی: گروه موجود (یا شناسهٔ گروه تازه) و انتخاب‌های مقصد آن.
    /// </summary>
    /// <param name="Existing">گروه موجود همین پیش‌نویس؛ تهی برای گروه تازه.</param>
    /// <param name="NewGroupId">شناسهٔ قطعی گروه تازه.</param>
    /// <param name="Nodes">انتخاب‌های مقصد.</param>
    public sealed record RegionGroupAssignment(
        ShippingAuthoringRegionGroup? Existing,
        Guid? NewGroupId,
        IReadOnlyList<ShippingDestinationNode> Nodes);

    /// <summary>
    /// کل درخت پیشنهادی را <b>بدون هیچ تغییر</b> روی aggregate می‌سنجد: شکل، عضویت، یکتایی شناسهٔ
    /// گروه‌های باقی‌مانده و یکتایی گره‌ها. هر نقصی پیش از اولین Apply/Order/Revision/timestamp بالا
    /// می‌آید تا رد شدن درخواست هیچ ردیف tracked را تغییر نداده باشد.
    /// </summary>
    /// <param name="regionGroups">درخت پیشنهادی.</param>
    public IReadOnlyList<RegionGroupAssignment> PreflightRegionGroups(
        IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups)
    {
        ArgumentNullException.ThrowIfNull(regionGroups);
        ValidateShape(regionGroups);

        var byId = _regionGroups.ToDictionary(group => group.GroupId);
        var seenGroups = new HashSet<Guid>();
        var seenNodes = new HashSet<ShippingDestinationNode>();
        var plan = new List<RegionGroupAssignment>(regionGroups.Count);

        foreach (var input in regionGroups)
        {
            ShippingAuthoringRegionGroup? existing = null;
            Guid? newGroupId = null;
            if (input.GroupId is { } groupId)
            {
                // همان شناسه دو بار در یک درخواست، یا شناسه‌ای که به این پیش‌نویس تعلق ندارد، هرگز
                // دوباره وصل نمی‌شود.
                if (!seenGroups.Add(groupId) || !byId.TryGetValue(groupId, out existing))
                {
                    throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
                }
            }
            else
            {
                newGroupId = UuidV7.New();
            }

            foreach (var node in input.Selections)
            {
                // یک گره دقیقاً یک بار در کل پیش‌نویس: ردیف‌های انتخاب با (draft_id, node_kind, node_id)
                // یکتا هستند و اینجا پیش از هر نوشتنی رد می‌شود.
                if (!seenNodes.Add(node))
                {
                    throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionSelectionOverlap));
                }
            }

            plan.Add(new RegionGroupAssignment(existing, newGroupId, input.Selections));
        }

        return plan;
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شده را اعمال می‌کند. هیچ اعتبارسنجی‌ای اینجا انجام نمی‌شود؛ ورودی باید از
    /// <see cref="PreflightRegionGroups"/> آمده باشد.
    /// </summary>
    /// <param name="plan">برنامهٔ معتبر.</param>
    /// <param name="staged">آیا ترتیب موقت مرحلهٔ اول نوشته شود.</param>
    /// <param name="now">زمان UTC.</param>
    public void ApplyRegionGroups(
        IReadOnlyList<RegionGroupAssignment> plan,
        bool staged,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var existingAreas = new Dictionary<ShippingDestinationNode, ShippingAuthoringSelectedArea>();
        foreach (var group in _regionGroups)
        {
            foreach (var area in group.DetachAreas())
            {
                existingAreas[area.Node] = area;
            }
        }

        var groups = new List<ShippingAuthoringRegionGroup>(plan.Count);
        for (var index = 0; index < plan.Count; index++)
        {
            var assignment = plan[index];
            var group = assignment.Existing
                ?? ShippingAuthoringRegionGroup.Create(DraftId, assignment.NewGroupId!.Value, 0);

            var next = new List<ShippingAuthoringSelectedArea>(assignment.Nodes.Count);
            foreach (var node in assignment.Nodes)
            {
                if (existingAreas.Remove(node, out var kept))
                {
                    kept.Retarget(group.GroupId);
                    next.Add(kept);
                }
                else
                {
                    next.Add(ShippingAuthoringSelectedArea.Create(DraftId, group.GroupId, node));
                }
            }

            group.ReplaceAreas(staged ? StagedOrderBase + index : index, next);
            groups.Add(group);
        }

        // گروه‌ها/انتخاب‌هایی که در درخت جدید نیستند از aggregate حذف می‌شوند؛ حذف واقعی در همان
        // SaveChanges انجام می‌شود و هیچ گروه دیگری را از مالک دیگر جابه‌جا نمی‌کند.
        _regionGroups.Clear();
        _regionGroups.AddRange(groups);
        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    /// <summary>
    /// ترتیب نهایی صفرپایه را در مرحلهٔ دوم جایگزینی می‌نشاند. تنها Order تغییر می‌کند؛ هویت،
    /// انتخاب‌ها، بازبینی و زمان دست‌نخورده می‌مانند.
    /// </summary>
    public void ApplyFinalRegionGroupOrder()
    {
        for (var index = 0; index < _regionGroups.Count; index++)
        {
            _regionGroups[index].SetOrder(index);
        }
    }

    /// <summary>گروه با شناسهٔ داده‌شده در همین پیش‌نویس، یا <c>null</c>.</summary>
    /// <param name="groupId">شناسهٔ گروه.</param>
    public ShippingAuthoringRegionGroup? FindRegionGroup(Guid groupId)
        => _regionGroups.SingleOrDefault(group => group.GroupId == groupId);

    /// <summary>
    /// یک ردیف نرخ دستی در بدنهٔ نوشتن، پیش از هر قاعدهٔ دامنه. مقادیر nullable صرفاً شکل انتقال‌اند:
    /// پیش‌سنجی دامنه آن‌ها را به یک مقدار متعارف تبدیل می‌کند یا کل مجموعه را رد می‌کند.
    /// </summary>
    /// <param name="RateRowId">شناسهٔ ردیف موجود همین گروه؛ تهی برای ردیف تازه.</param>
    /// <param name="MinWeightGrams">کران پایین وزن.</param>
    /// <param name="MaxWeightGrams">کران بالای وزن.</param>
    /// <param name="MinItemCount">کران پایین تعداد واحد.</param>
    /// <param name="MaxItemCount">کران بالای تعداد واحد.</param>
    /// <param name="MinMerchandiseAmount">کران پایین مبلغ کالا.</param>
    /// <param name="MaxMerchandiseAmount">کران بالای مبلغ کالا.</param>
    /// <param name="FreightAmount">هزینهٔ حمل.</param>
    /// <param name="PackagingAmount">هزینهٔ بسته‌بندی.</param>
    /// <param name="MinDeliveryDays">کمینهٔ مدت تحویل.</param>
    /// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل.</param>
    public sealed record ShippingAuthoringRateRowInput(
        Guid? RateRowId,
        long? MinWeightGrams,
        long? MaxWeightGrams,
        int? MinItemCount,
        int? MaxItemCount,
        decimal? MinMerchandiseAmount,
        decimal? MaxMerchandiseAmount,
        decimal? FreightAmount,
        decimal? PackagingAmount,
        int? MinDeliveryDays,
        int? MaxDeliveryDays);

    /// <summary>یک جای‌گذاری ردیف در برنامهٔ پیش‌سنجی‌شده: ردیف موجود (برای حفظ هویت) یا ردیف تازه.</summary>
    /// <param name="Existing">ردیف موجود همین گروه؛ تهی برای ردیف تازه.</param>
    /// <param name="Value">مقدار متعارف تأییدشده.</param>
    public sealed record RegionRateRowAssignment(
        ShippingAuthoringRateRow? Existing,
        ShippingAuthoringRateRowValue Value);

    /// <summary>برنامهٔ کامل و تأییدشدهٔ جایگزینی ردیف‌های یک گروه.</summary>
    /// <param name="Group">گروه مالک.</param>
    /// <param name="CurrencyCode">ارز bind‌شده پس از این نوشتن (ممکن است تهی بماند).</param>
    /// <param name="Rows">ردیف‌های تأییدشده.</param>
    public sealed record GroupRateRowPlan(
        ShippingAuthoringRegionGroup Group,
        string? CurrencyCode,
        IReadOnlyList<RegionRateRowAssignment> Rows);

    /// <summary>
    /// کل مجموعهٔ ردیف‌های نرخ یک گروه را <b>بدون هیچ تغییر</b> می‌سنجد: هویت ردیف‌ها، بازه‌های
    /// نیم‌باز، مبلغ/دقت canonical، مدت تحویل، هم‌پوشانی کامل سه‌محوری و ارز. اگر هر ردیفی نامعتبر
    /// باشد، هیچ ردیف/بازبینی/زمان/ارزی تغییر نمی‌کند و ردیف‌های قبلی هم دست‌نخورده می‌مانند.
    /// </summary>
    /// <param name="groupId">گروه مالک.</param>
    /// <param name="rows">مجموعهٔ کامل ردیف‌های پیشنهادی.</param>
    /// <param name="trustedCurrencyCode">ارز معتبر سرورساخته (نه از transport).</param>
    public GroupRateRowPlan PreflightGroupRateRows(
        Guid groupId,
        IReadOnlyList<ShippingAuthoringRateRowInput> rows,
        string trustedCurrencyCode)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var group = FindRegionGroup(groupId)
            ?? throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionGroupNotFound));

        if (rows.Count > ShippingAuthoringRateRow.MaxRowsPerGroup)
        {
            ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateRowPayloadInvalid);
        }

        var currency = NormalizeTrustedCurrency(trustedCurrencyCode);
        var existingById = group.RateRows.ToDictionary(row => row.RateRowId);
        var seenIds = new HashSet<Guid>();
        var assignments = new List<RegionRateRowAssignment>(rows.Count);

        foreach (var input in rows)
        {
            if (input is null)
            {
                ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateRowPayloadInvalid);
            }

            ShippingAuthoringRateRow? existing = null;
            if (input.RateRowId is { } rowId)
            {
                // A retained id is a claim: it must be a real row of THIS group and may appear once.
                if (rowId == Guid.Empty
                    || !seenIds.Add(rowId)
                    || !existingById.TryGetValue(rowId, out existing))
                {
                    ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateRowPayloadInvalid);
                }
            }

            var value = NormalizeRow(input);
            if (assignments.Any(assignment => assignment.Value.Overlaps(value)))
            {
                ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateOverlap);
            }

            assignments.Add(new RegionRateRowAssignment(existing, value));
        }

        // A bound currency is the truth of the stored amounts. A different trusted currency is a
        // configuration mismatch, never a silent reinterpretation or conversion of old numbers.
        if (group.CurrencyCode is { } stored
            && !string.Equals(stored, currency, StringComparison.Ordinal))
        {
            ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateCurrencyMismatch);
        }

        // The currency binds on the first NON-EMPTY row set and is never cleared, so removing rows does
        // not turn the stored amounts into "no currency" or another currency.
        var nextCurrency = group.CurrencyCode ?? (assignments.Count > 0 ? currency : null);
        return new GroupRateRowPlan(group, nextCurrency, assignments);
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شده را اعمال می‌کند. هیچ اعتبارسنجی‌ای اینجا انجام نمی‌شود؛ ورودی باید از
    /// <see cref="PreflightGroupRateRows"/> آمده باشد. بازبینی ریشه یک واحد بالا می‌رود تا نوشتن
    /// ردیف‌ها و جایگزینی درخت با یک نشانگر همزمانی مشترک دیده شوند.
    /// </summary>
    /// <param name="plan">برنامهٔ معتبر.</param>
    /// <param name="now">زمان UTC.</param>
    public void ApplyGroupRateRows(GroupRateRowPlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(plan.Group);

        var next = new List<ShippingAuthoringRateRow>(plan.Rows.Count);
        foreach (var assignment in plan.Rows)
        {
            if (assignment.Existing is { } kept)
            {
                kept.Apply(assignment.Value);
                next.Add(kept);
            }
            else
            {
                next.Add(ShippingAuthoringRateRow.Create(plan.Group.GroupId, assignment.Value));
            }
        }

        plan.Group.ReplaceRateRows(next);
        if (plan.CurrencyCode is { } currency)
        {
            plan.Group.BindCurrency(currency);
        }

        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    /// <summary>پیش‌سنجی و اعمال یک‌جای مجموعهٔ ردیف‌های یک گروه.</summary>
    /// <param name="groupId">گروه مالک.</param>
    /// <param name="rows">مجموعهٔ کامل ردیف‌ها.</param>
    /// <param name="trustedCurrencyCode">ارز معتبر سرورساخته.</param>
    /// <param name="now">زمان UTC.</param>
    public void ReplaceGroupRateRows(
        Guid groupId,
        IReadOnlyList<ShippingAuthoringRateRowInput> rows,
        string trustedCurrencyCode,
        DateTimeOffset now)
    {
        var plan = PreflightGroupRateRows(groupId, rows, trustedCurrencyCode);
        ApplyGroupRateRows(plan, now);
    }

    /// <summary>یک تغییر نام/توضیح برای یک زبان محتوا (ورودی دامنه، بدون قاعدهٔ transport).</summary>
    /// <param name="LanguageId">شناسهٔ زبان محتوا.</param>
    /// <param name="Name">نام همان زبان.</param>
    /// <param name="Description">توضیح همان زبان یا تهی.</param>
    public sealed record MethodTranslationInput(Guid LanguageId, string? Name, string? Description);

    /// <summary>یک تغییر/پاک‌کردن عنوان یک گروه برای یک زبان.</summary>
    /// <param name="GroupId">شناسهٔ گروه همین پیش‌نویس.</param>
    /// <param name="LanguageId">شناسهٔ زبان محتوا.</param>
    /// <param name="Title">عنوان یا تهی برای پاک‌کردن همان جفت.</param>
    public sealed record GroupTitleInput(Guid GroupId, Guid LanguageId, string? Title);

    /// <summary>یک جای‌گذاری ترجمهٔ نام: ردیف موجود (برای حفظ هویت) یا ردیف تازه.</summary>
    /// <param name="Existing">ترجمهٔ موجود همان زبان؛ تهی برای زبان تازه.</param>
    /// <param name="LanguageId">شناسهٔ زبان.</param>
    /// <param name="Name">نام تأییدشده.</param>
    /// <param name="Description">توضیح تأییدشده یا تهی.</param>
    public sealed record MethodTranslationAssignment(
        ShippingAuthoringDraftTranslation? Existing,
        Guid LanguageId,
        string Name,
        string? Description);

    /// <summary>یک جای‌گذاری عنوان گروه؛ عنوان تهی یعنی پاک‌کردن همان جفت.</summary>
    /// <param name="Group">گروه مالک.</param>
    /// <param name="Existing">عنوان موجود همان جفت؛ تهی برای تازه/پاک‌نشده.</param>
    /// <param name="LanguageId">شناسهٔ زبان.</param>
    /// <param name="Title">عنوان تأییدشده یا تهی.</param>
    public sealed record GroupTitleAssignment(
        ShippingAuthoringRegionGroup Group,
        ShippingAuthoringRegionGroupTitle? Existing,
        Guid LanguageId,
        string? Title);

    /// <summary>برنامهٔ کامل و تأییدشدهٔ به‌روزرسانی ارائه (بدون هیچ تغییر روی aggregate).</summary>
    /// <param name="MethodTranslations">تغییرهای نام/توضیح.</param>
    /// <param name="GroupTitles">تغییرهای عنوان گروه.</param>
    public sealed record PresentationPlan(
        IReadOnlyList<MethodTranslationAssignment> MethodTranslations,
        IReadOnlyList<GroupTitleAssignment> GroupTitles)
    {
        /// <summary>آیا پیش‌سنجی حداقل یک تغییر واقعی دید (برای no-op صادقانه).</summary>
        public bool HasChanges { get; init; }
    }

    /// <summary>
    /// کل سند ارائه را <b>بدون هیچ تغییر</b> می‌سنجد: شکل، یکتایی، عضویت گروه، شناسهٔ زبان فعال و
    /// متن هر ورودی. هر نقصی پیش از اولین نوشتن برمی‌گردد، پس ردیف‌های tracked و بازبینی دست‌نخورده
    /// می‌مانند.
    /// </summary>
    /// <remarks>
    /// لیست‌های خالی یعنی «هیچ تغییر» (no-op) و مجاز است. فقط زبان‌های <b>فعال</b> در همین registry
    /// می‌توانند نوشته شوند؛ هر ورودی با زبان غیرفعال — حتی با متن یکسان و از قبل ذخیره‌شده — پیش از
    /// هر تغییر با <c>shipping.authoring.presentation_language_invalid</c> رد می‌شود. متنِ ذخیره‌شدهٔ
    /// زبانی که بعداً غیرفعال شده حذف یا ترجمه نمی‌شود و صرفاً به‌صورت خام و خواندنی باقی می‌ماند
    /// (retention غیرفعال/گذرا)، پس این متد هیچ استثنای نوشتن برای زبان غیرفعال ندارد.
    /// </remarks>
    /// <param name="methodTranslations">تغییرهای نام/توضیح.</param>
    /// <param name="groupTitles">تغییرهای عنوان گروه.</param>
    /// <param name="activeLanguageIds">شناسهٔ زبان‌های فعال authoritative (سرورساخته).</param>
    public PresentationPlan PreflightPresentation(
        IReadOnlyList<MethodTranslationInput> methodTranslations,
        IReadOnlyList<GroupTitleInput> groupTitles,
        IReadOnlySet<Guid> activeLanguageIds)
    {
        ArgumentNullException.ThrowIfNull(methodTranslations);
        ArgumentNullException.ThrowIfNull(groupTitles);
        ArgumentNullException.ThrowIfNull(activeLanguageIds);

        var existingByLanguage = _translations.ToDictionary(translation => translation.LanguageId);
        var seenLanguages = new HashSet<Guid>();
        var translationPlan = new List<MethodTranslationAssignment>(methodTranslations.Count);
        var changed = false;

        foreach (var input in methodTranslations)
        {
            if (input is null || input.LanguageId == Guid.Empty || !seenLanguages.Add(input.LanguageId))
            {
                RejectPresentation();
            }

            if (!activeLanguageIds.Contains(input.LanguageId))
            {
                // Previously stored text for a now-inactive language is retained, not re-edited: only
                // an ACTIVE language may receive a write.
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringPresentationLanguageInvalid));
            }

            var name = ShippingAuthoringPresentationRules.NormalizeName(input.Name);
            var description = ShippingAuthoringPresentationRules.NormalizeDescription(input.Description);
            existingByLanguage.TryGetValue(input.LanguageId, out var existing);
            if (existing is null
                || !string.Equals(existing.Name, name, StringComparison.Ordinal)
                || !string.Equals(existing.Description, description, StringComparison.Ordinal))
            {
                changed = true;
            }

            translationPlan.Add(new MethodTranslationAssignment(existing, input.LanguageId, name, description));
        }

        var titlePlan = new List<GroupTitleAssignment>(groupTitles.Count);
        var seenGroupLanguages = new HashSet<(Guid GroupId, Guid LanguageId)>();
        foreach (var input in groupTitles)
        {
            if (input is null || input.GroupId == Guid.Empty || input.LanguageId == Guid.Empty
                || !seenGroupLanguages.Add((input.GroupId, input.LanguageId)))
            {
                RejectPresentation();
            }

            var group = FindRegionGroup(input.GroupId);
            if (group is null)
            {
                // A foreign/invented group id is the same safe answer as a missing one, before any write.
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionGroupNotFound));
            }

            if (!activeLanguageIds.Contains(input.LanguageId))
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringPresentationLanguageInvalid));
            }

            var title = ShippingAuthoringPresentationRules.NormalizeTitle(input.Title);
            var existing = group.Titles.SingleOrDefault(candidate => candidate.LanguageId == input.LanguageId);
            if (title is null)
            {
                if (existing is not null)
                {
                    changed = true;
                }
            }
            else if (existing is null || !string.Equals(existing.Title, title, StringComparison.Ordinal))
            {
                changed = true;
            }

            titlePlan.Add(new GroupTitleAssignment(group, existing, input.LanguageId, title));
        }

        return new PresentationPlan(translationPlan, titlePlan) { HasChanges = changed };
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شده را اعمال می‌کند. هیچ اعتبارسنجی‌ای اینجا نیست؛ ورودی باید از
    /// <see cref="PreflightPresentation"/> آمده باشد. اگر هیچ مقدار واقعی تغییر نکند، بازبینی و زمان
    /// هم تغییر نمی‌کنند (no-op صادقانه).
    /// </summary>
    /// <param name="plan">برنامهٔ معتبر.</param>
    /// <param name="now">زمان UTC.</param>
    public void ApplyPresentation(PresentationPlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var changed = false;
        foreach (var assignment in plan.MethodTranslations)
        {
            if (assignment.Existing is { } kept)
            {
                if (!string.Equals(kept.Name, assignment.Name, StringComparison.Ordinal)
                    || !string.Equals(kept.Description, assignment.Description, StringComparison.Ordinal))
                {
                    kept.Apply(assignment.Name, assignment.Description);
                    changed = true;
                }
            }
            else
            {
                _translations.Add(ShippingAuthoringDraftTranslation.Create(
                    DraftId,
                    assignment.LanguageId,
                    assignment.Name,
                    assignment.Description));
                changed = true;
            }
        }

        foreach (var assignment in plan.GroupTitles)
        {
            changed |= assignment.Group.SetTitle(assignment.LanguageId, assignment.Title);
        }

        if (!changed)
        {
            return;
        }

        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    /// <summary>یک روز هفتهٔ ورودی برنامه: پرچم صریح کاری/تعطیل و ساعت‌های اختیاری همان روز.</summary>
    /// <param name="Weekday">روز هفتهٔ ISO ۱..۷.</param>
    /// <param name="IsWorking">پرچم صریح؛ نبود آن در لایهٔ انتقال پذیرفته نمی‌شود.</param>
    /// <param name="StartTime">شروع بازهٔ محلی؛ شامل و برای روز کاری الزامی.</param>
    /// <param name="EndTime">پایان بازهٔ محلی؛ غیرشامل و برای روز کاری الزامی.</param>
    public sealed record ScheduleDayInput(int Weekday, bool IsWorking, TimeOnly? StartTime, TimeOnly? EndTime);

    /// <summary>یک روز پس از پیش‌سنجی: ردیف موجود (برای حفظ هویت) یا تهی برای نخستین پیکربندی.</summary>
    /// <param name="Existing">ردیف موجود همین روز در همین پیش‌نویس.</param>
    /// <param name="Weekday">روز هفتهٔ ISO.</param>
    /// <param name="IsWorking">پرچم تأییدشده.</param>
    /// <param name="StartTime">ساعت شروع پس از حفظ/جایگزینی.</param>
    /// <param name="EndTime">ساعت پایان پس از حفظ/جایگزینی.</param>
    public sealed record ScheduleDayAssignment(
        ShippingAuthoringScheduleDay? Existing,
        int Weekday,
        bool IsWorking,
        TimeOnly? StartTime,
        TimeOnly? EndTime);

    /// <summary>برنامهٔ کامل و تأییدشدهٔ جایگزینی هفته (بدون هیچ تغییر روی aggregate).</summary>
    /// <param name="TimeZoneId">منطقهٔ زمانی نرمال‌شده و قابل‌حل.</param>
    /// <param name="Days">دقیقاً هفت روز.</param>
    public sealed record WeeklySchedulePlan(string TimeZoneId, IReadOnlyList<ScheduleDayAssignment> Days)
    {
        /// <summary>
        /// آیا این جایگزینی دست‌کم یک تغییر واقعی دارد. یک PUT یکسان (همان منطقهٔ زمانی و همان روزها)
        /// یک no-op صادقانه است: بازبینی و زمان تغییر نمی‌کنند، اما <c>expectedRevision</c> همیشه
        /// بررسی می‌شود.
        /// </summary>
        public bool HasChanges { get; init; }
    }

    /// <summary>
    /// کل هفته را <b>بدون هیچ تغییر</b> می‌سنجد: منطقهٔ زمانی، تعداد دقیق روزها، یکتایی/بازهٔ روز هفته،
    /// حضور پرچم کاری و جفت ساعت‌ها. هر نقصی پیش از نخستین نوشتن برمی‌گردد، پس ردیف‌های tracked،
    /// بازبینی، زمان و سایر قابلیت‌ها دست‌نخورده می‌مانند.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>هفت روز، نه کمتر.</b> روز حذف‌شده «تعطیل» نیست؛ یک پیکربندی ناقص است و با کد پایدار رد
    /// می‌شود. روز خارج از ۱..۷ همان کد قدیمی <c>shipping.schedule.weekday.invalid</c> را می‌گیرد.
    /// </para>
    /// <para>
    /// <b>ساعت‌ها.</b> روز کاری باید هر دو ساعت را داشته باشد و <c>end &gt; start</c> باشد؛ بازهٔ
    /// معکوس/مساوی/شبانه با کد قدیمی <c>shipping.schedule.window.range_invalid</c> رد می‌شود. روز
    /// تعطیل با هر دو ساعت تهی، جفتِ ذخیره‌شدهٔ قبلی همان روز را حفظ می‌کند؛ یک ساعت بدون دیگری
    /// نامعتبر است. هیچ نرمال‌سازی، معکوس‌سازی یا کشیدن بازه به روز بعد وجود ندارد.
    /// </para>
    /// <para>
    /// <b>هیچ استنتاج منطقهٔ زمانی.</b> شناسه با <see cref="ShippingScheduleRules.NormalizeTimeZone"/>
    /// اعتبارسنجی می‌شود؛ locale/کشور/ساعت مرورگر یا پیش‌فرض سخت‌کدشده دخالت ندارد.
    /// </para>
    /// </remarks>
    /// <param name="timeZoneId">شناسهٔ منطقهٔ زمانی از بدنه.</param>
    /// <param name="days">مجموعهٔ هفت‌روزهٔ پیشنهادی.</param>
    public WeeklySchedulePlan PreflightWeeklySchedule(string? timeZoneId, IReadOnlyList<ScheduleDayInput> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        var normalizedTimeZone = ShippingScheduleRules.NormalizeTimeZone(timeZoneId);
        if (days.Count != ShippingAuthoringWeeklySchedule.DaysPerWeek)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid));
        }

        var existingByWeekday = WeeklySchedule is { } schedule
            ? schedule.Days.ToDictionary(day => day.Weekday)
            : new Dictionary<int, ShippingAuthoringScheduleDay>();
        var seen = new HashSet<int>();
        var assignments = new List<ScheduleDayAssignment>(ShippingAuthoringWeeklySchedule.DaysPerWeek);
        foreach (var day in days)
        {
            if (day is null)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
            }

            var weekday = ShippingOperatingWindow.NormalizeWeekday(day.Weekday);
            if (!seen.Add(weekday))
            {
                // A duplicate weekday would make "which row is Tuesday" depend on array order.
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid));
            }

            existingByWeekday.TryGetValue(weekday, out var existing);
            var (start, end) = NormalizeScheduleDayHours(day.IsWorking, day.StartTime, day.EndTime, existing);
            assignments.Add(new ScheduleDayAssignment(existing, weekday, day.IsWorking, start, end));
        }

        if (seen.Count != ShippingAuthoringWeeklySchedule.DaysPerWeek)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid));
        }

        var unchanged = WeeklySchedule is { } current
                         && string.Equals(current.TimeZoneId, normalizedTimeZone, StringComparison.Ordinal)
                         && assignments.All(assignment => assignment.Existing is { } row
                                                          && row.Matches(
                                                              assignment.IsWorking,
                                                              assignment.StartTime,
                                                              assignment.EndTime));
        return new WeeklySchedulePlan(normalizedTimeZone, assignments) { HasChanges = !unchanged };
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شده را اعمال می‌کند. هیچ اعتبارسنجی‌ای اینجا نیست؛ ورودی باید از
    /// <see cref="PreflightWeeklySchedule"/> آمده باشد. جایگزینی کامل هفته یک واحد افزایش بازبینی
    /// ریشه است و با همان SaveChanges ضمنی نوشته می‌شود؛ PUT یکسان بازبینی/زمان را تغییر نمی‌دهد.
    /// </summary>
    /// <param name="plan">برنامهٔ معتبر.</param>
    /// <param name="now">زمان UTC.</param>
    public void ApplyWeeklySchedule(WeeklySchedulePlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.HasChanges)
        {
            return;
        }

        if (WeeklySchedule is { } schedule)
        {
            schedule.Apply(plan.TimeZoneId, plan.Days, now);
        }
        else
        {
            WeeklySchedule = ShippingAuthoringWeeklySchedule.Create(DraftId, plan.TimeZoneId, plan.Days, now);
        }

        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    /// <summary>
    /// ساعت‌های یک روز را تعیین می‌کند: روز کاری هر دو ساعت را می‌خواهد، روز تعطیل با هر دو تهی جفت
    /// ذخیره‌شده را حفظ می‌کند، و یک ساعت تنها یا بدون جفت معتبر رد می‌شود.
    /// </summary>
    private static (TimeOnly? Start, TimeOnly? End) NormalizeScheduleDayHours(
        bool isWorking,
        TimeOnly? startTime,
        TimeOnly? endTime,
        ShippingAuthoringScheduleDay? existing)
    {
        if (isWorking)
        {
            if (startTime is not { } workingStart || endTime is not { } workingEnd)
            {
                // A working day without an explicit interval would silently invent a schedule.
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
            }

            return (workingStart, ShippingOperatingWindow.NormalizeRange(workingStart, workingEnd));
        }

        if (startTime is null && endTime is null)
        {
            return existing is { } kept ? (kept.StartTime, kept.EndTime) : (null, null);
        }

        if (startTime is not { } closedStart || endTime is not { } closedEnd)
        {
            // One supplied bound is not a remembered pair and must not be half-stored.
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
        }

        return (closedStart, ShippingOperatingWindow.NormalizeRange(closedStart, closedEnd));
    }

    /// <summary>
    /// برنامهٔ کامل و تأییدشدهٔ تنظیمات روش: دو مقدار تعریف‌شده و صریح.
    /// </summary>
    /// <param name="FreightPaymentMode">زمان پرداخت کرایه.</param>
    /// <param name="CalculationMode">حالت محاسبهٔ کرایه.</param>
    public sealed record MethodSettingsPlan(
        ShippingFreightPayerMode FreightPaymentMode,
        ShippingAuthoringCalculationMode CalculationMode)
    {
        /// <summary>
        /// آیا این جایگزینی دست‌کم یک تغییر واقعی دارد. یک PUT یکسان (همان دو انتخاب) یک no-op
        /// صادقانه است: بازبینی و زمان تغییر نمی‌کنند، اما <c>expectedRevision</c> همیشه بررسی می‌شود.
        /// </summary>
        public bool HasChanges { get; init; }
    }

    /// <summary>
    /// تنظیمات روش را <b>بدون هیچ تغییر</b> روی aggregate می‌سنجد: حضور صریح هر دو انتخاب و تعریف‌شده
    /// بودن آن‌ها. هر نقصی پیش از نخستین نوشتن برمی‌گردد، پس ردیف‌های tracked، بازبینی، زمان و سایر
    /// قابلیت‌ها دست‌نخورده می‌مانند.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>غایب ≠ صفر.</b> مقدار تهی یا عددی خارج از مقادیر تعریف‌شده با کد پایدار رد می‌شود؛ هیچ
    /// «پیش‌فرض پیش‌پرداخت/دستی» ساخته نمی‌شود.
    /// </para>
    /// <para>
    /// <b>دو انتخاب مستقل.</b> این متد هیچ رابطه‌ای بین زمان پرداخت و حالت محاسبه تحمیل نمی‌کند: هر
    /// شش ترکیب مجاز است. تغییر یکی، دیگری و همهٔ ردیف‌های دستی/ارز/متن/جغرافیا/برنامه را دست نمی‌زند.
    /// </para>
    /// </remarks>
    /// <param name="freightPaymentMode">زمان پرداخت کرایه از بدنه.</param>
    /// <param name="calculationMode">حالت محاسبه از بدنه.</param>
    public MethodSettingsPlan PreflightMethodSettings(
        ShippingFreightPayerMode? freightPaymentMode,
        ShippingAuthoringCalculationMode? calculationMode)
    {
        if (freightPaymentMode is not { } freight
            || !Enum.IsDefined(freight)
            || calculationMode is not { } calculation
            || !Enum.IsDefined(calculation))
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringMethodSettingsInvalid));
        }

        var unchanged = MethodSettings is { } current
                        && current.Matches(freight, calculation);
        return new MethodSettingsPlan(freight, calculation) { HasChanges = !unchanged };
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شدهٔ انتخاب نوع ارسال: شناسهٔ پایدار (یا تهی برای پاک‌کردن).
    /// </summary>
    /// <param name="ShippingTypeId">شناسهٔ تعریف نوع یا تهی.</param>
    public sealed record TypeAssignmentPlan(Guid? ShippingTypeId)
    {
        /// <summary>
        /// آیا این انتخاب دست‌کم یک تغییر واقعی دارد. یک PUT یکسان (همان شناسهٔ ذخیره‌شده) یک no-op
        /// صادقانه است: بازبینی و زمان تغییر نمی‌کنند، اما <c>expectedRevision</c> همیشه بررسی می‌شود.
        /// </summary>
        public bool HasChanges { get; init; }
    }

    /// <summary>
    /// انتخاب نوع ارسال را <b>بدون هیچ تغییر</b> روی aggregate می‌سنجد. شناسهٔ تهی (Guid.Empty) هرگز
    /// «انتخاب‌نشده» نیست، یک payload نامعتبر است و پیش از نخستین نوشتن رد می‌شود.
    /// </summary>
    /// <remarks>
    /// تصمیم‌های بیرونی (وجود/فعال‌بودن تعریف) اینجا تکرار نمی‌شوند: دامنه فقط شکل محلی را می‌سنجد.
    /// پاک‌کردن با تهی هیچ فراخوان بیرونی نمی‌خواهد و همیشه مجاز است.
    /// </remarks>
    /// <param name="shippingTypeId">شناسهٔ تعریف انتخاب‌شده یا تهی برای پاک‌کردن.</param>
    public TypeAssignmentPlan PreflightTypeAssignment(Guid? shippingTypeId)
    {
        if (shippingTypeId is { } candidate && candidate == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringTypeAssignmentInvalid));
        }

        return new TypeAssignmentPlan(shippingTypeId) { HasChanges = ShippingTypeId != shippingTypeId };
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شدهٔ انتخاب نوع را اعمال می‌کند. هیچ اعتبارسنجی‌ای اینجا نیست؛ ورودی باید از
    /// <see cref="PreflightTypeAssignment"/> آمده باشد. تغییر واقعی بازبینی ریشه را یک واحد بالا
    /// می‌برد و فقط شناسه/بازبینی/زمان را می‌نویسد.
    /// </summary>
    /// <param name="plan">برنامهٔ معتبر.</param>
    /// <param name="now">زمان UTC.</param>
    public void ApplyTypeAssignment(TypeAssignmentPlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.HasChanges)
        {
            return;
        }

        ShippingTypeId = plan.ShippingTypeId;
        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    /// <summary>
    /// برنامهٔ پیش‌سنجی‌شده را اعمال می‌کند. هیچ اعتبارسنجی‌ای اینجا نیست؛ ورودی باید از
    /// <see cref="PreflightMethodSettings"/> آمده باشد. تغییر واقعی، بازبینی ریشه را یک واحد بالا
    /// می‌برد و در همان SaveChanges ضمنی با سایر قابلیت‌ها نوشته می‌شود.
    /// </summary>
    /// <param name="plan">برنامهٔ معتبر.</param>
    /// <param name="now">زمان UTC.</param>
    public void ApplyMethodSettings(MethodSettingsPlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (!plan.HasChanges)
        {
            return;
        }

        if (MethodSettings is { } settings)
        {
            settings.Apply(plan.FreightPaymentMode, plan.CalculationMode, now);
        }
        else
        {
            MethodSettings = ShippingAuthoringMethodSettings.Create(
                DraftId,
                plan.FreightPaymentMode,
                plan.CalculationMode,
                now);
        }

        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    private static void RejectPresentation()
        => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid));
    private static ShippingAuthoringRateRowValue NormalizeRow(ShippingAuthoringRateRowInput input)
    {
        var weightMin = NormalizeIntervalLong(
            input.MinWeightGrams,
            input.MaxWeightGrams,
            0,
            ShippingAuthoringRateRowRules.WeightGramsMaxValue,
            ShippingErrorCodes.AuthoringRateConditionInvalid);

        // The count minimum is a real unit count: zero units is not a group.
        var countMin = NormalizeIntervalLong(
            input.MinItemCount,
            input.MaxItemCount,
            1,
            ShippingAuthoringRateRowRules.ItemCountMaxValue,
            ShippingErrorCodes.AuthoringRateConditionInvalid);

        var amountMin = NormalizeIntervalAmount(input.MinMerchandiseAmount, input.MaxMerchandiseAmount);

        var freight = input.FreightAmount
            ?? throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid));
        var packaging = input.PackagingAmount
            ?? throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid));

        // The transport validator already refuses an omitted amount; a direct caller gets the same
        // refusal instead of a silent zero fee.
        var normalizedFreight = NormalizeAmount(freight);
        var normalizedPackaging = NormalizeAmount(packaging);

        var minDays = input.MinDeliveryDays
            ?? throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid));
        var maxDays = input.MaxDeliveryDays
            ?? throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid));

        if (minDays < 0 || minDays > ShippingAuthoringRateRowRules.DaysMaxValue
            || maxDays < minDays || maxDays > ShippingAuthoringRateRowRules.DaysMaxValue)
        {
            ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateDurationInvalid);
        }

        return new ShippingAuthoringRateRowValue(
            weightMin.Min,
            weightMin.Max,
            countMin.Min is { } count ? (int)count : null,
            countMin.Max is { } countMax ? (int)countMax : null,
            amountMin.Min,
            amountMin.Max,
            normalizedFreight,
            normalizedPackaging,
            minDays,
            maxDays);
    }

    private static (long? Min, long? Max) NormalizeIntervalLong(
        long? min,
        long? max,
        long minimumAllowed,
        long maximumAllowed,
        string code)
    {
        if (min is not { } lower)
        {
            // An upper bound alone is a half-written axis: "up to N" is expressed as min = 0.
            if (max is not null)
            {
                ShippingAuthoringRateRowRules.Reject(code);
            }

            return (null, null);
        }

        if (lower < minimumAllowed || lower > maximumAllowed)
        {
            ShippingAuthoringRateRowRules.Reject(code);
        }

        if (max is not { } upper)
        {
            return (lower, null);
        }

        if (upper <= lower || upper > maximumAllowed)
        {
            ShippingAuthoringRateRowRules.Reject(code);
        }

        return (lower, upper);
    }

    private static (decimal? Min, decimal? Max) NormalizeIntervalAmount(decimal? min, decimal? max)
    {
        if (min is not { } lower)
        {
            if (max is not null)
            {
                ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateConditionInvalid);
            }

            return (null, null);
        }

        var normalizedMin = NormalizeAmount(lower);
        if (max is not { } upper)
        {
            return (normalizedMin, null);
        }

        var normalizedMax = NormalizeAmount(upper);
        return normalizedMax <= normalizedMin
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRateConditionInvalid))
            : (normalizedMin, normalizedMax);
    }

    private static decimal NormalizeAmount(decimal amount)
    {
        if (amount < 0m
            || amount > ShippingAuthoringRateRowRules.AmountMaxValue
            || !ShippingAuthoringRateRowRules.IsRepresentableAmount(amount))
        {
            ShippingAuthoringRateRowRules.Reject(ShippingErrorCodes.AuthoringRateAmountInvalid);
        }

        return amount;
    }

    private static string NormalizeTrustedCurrency(string? currencyCode)
    {
        var trimmed = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        return trimmed.Length == 3 && trimmed.All(char.IsAsciiLetter)
            ? trimmed
            : throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringConfigurationUnavailable));
    }

    private void SetInitialGroups(IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups)
    {
        ValidateShape(regionGroups);
        for (var index = 0; index < regionGroups.Count; index++)
        {
            var group = ShippingAuthoringRegionGroup.Create(DraftId, UuidV7.New(), index);
            group.ReplaceAreas(
                index,
                regionGroups[index].Selections
                    .Select(node => ShippingAuthoringSelectedArea.Create(DraftId, group.GroupId, node))
                    .ToArray());
            _regionGroups.Add(group);
        }
    }

    private static void ValidateShape(IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups)
    {
        if (regionGroups.Count > MaxRegionGroups)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
        }

        var total = 0;
        foreach (var group in regionGroups)
        {
            if (group is null || group.Selections is null || group.Selections.Count > MaxSelectionsPerGroup)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
            }

            if (group.GroupId is { } id && id == Guid.Empty)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
            }

            total += group.Selections.Count;
            if (total > MaxSelectionsPerDraft)
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
            }

            if (group.Selections.Any(node => !node.IsValid))
            {
                throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
            }
        }
    }

    private static string NormalizeServiceCode(string? serviceCode)
    {
        var trimmed = serviceCode?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        return trimmed.Length > ServiceCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid))
            : trimmed;
    }

    private static string? NormalizeOptionCode(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > OptionCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.OptionCodeInvalid))
            : trimmed;
    }
}
