using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;

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

    private ShippingAuthoringRegionGroup()
    {
    }

    /// <summary>شناسهٔ پایدار گروه؛ مرجع آیندهٔ ردیف‌های نرخ/بسته‌بندی/زمان‌بندی.</summary>
    public Guid GroupId { get; private set; }

    /// <summary>شناسهٔ پیش‌نویس مالک (ستون denormalized همان قاعدهٔ یکتایی).</summary>
    public Guid DraftId { get; private set; }

    /// <summary>ترتیب صریح گروه در پیش‌نویس (صفرپایه).</summary>
    public int Order { get; private set; }

    /// <summary>انتخاب‌های این گروه، مرتب‌شده برای خواندن قطعی.</summary>
    public IReadOnlyList<ShippingAuthoringSelectedArea> Areas => _areas;

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

    private readonly List<ShippingAuthoringRegionGroup> _regionGroups = [];

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
