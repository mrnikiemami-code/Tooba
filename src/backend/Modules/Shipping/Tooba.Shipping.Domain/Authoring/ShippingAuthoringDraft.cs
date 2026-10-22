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
    /// ترتیب و انتخاب‌های این گروه را جایگزین می‌کند و ردیف‌های هم‌هویت موجود را دوباره استفاده
    /// می‌کند تا جایگزینی یک درخت، حذف/درج یکتایی‌شکن در همان SaveChanges نسازد.
    /// </summary>
    /// <param name="order">ترتیب جدید.</param>
    /// <param name="nodes">انتخاب‌های جدید.</param>
    internal void Apply(int order, IReadOnlyList<ShippingDestinationNode> nodes)
    {
        Order = order;
        var existing = _areas.ToDictionary(area => area.Node);
        var next = new List<ShippingAuthoringSelectedArea>(nodes.Count);
        foreach (var node in nodes)
        {
            if (existing.Remove(node, out var kept))
            {
                next.Add(kept);
                continue;
            }

            next.Add(ShippingAuthoringSelectedArea.Create(DraftId, GroupId, node));
        }

        _areas.Clear();
        _areas.AddRange(next);
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
        ArgumentNullException.ThrowIfNull(regionGroups);
        ValidateShape(regionGroups);

        var existing = _regionGroups.ToDictionary(group => group.GroupId);
        var retained = new List<ShippingAuthoringRegionGroup>(regionGroups.Count);
        for (var index = 0; index < regionGroups.Count; index++)
        {
            var input = regionGroups[index];
            ShippingAuthoringRegionGroup group;
            if (input.GroupId is { } groupId)
            {
                // یک شناسهٔ گروه که به این پیش‌نویس تعلق ندارد، هرگز نمی‌تواند دوباره وصل شود؛
                // درخواست رد می‌شود تا حذف/جایگزینی خاموش رخ ندهد.
                if (!existing.Remove(groupId, out group!))
                {
                    throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
                }
            }
            else
            {
                group = ShippingAuthoringRegionGroup.Create(DraftId, UuidV7.New(), index);
            }

            group.Apply(index, input.Selections);
            retained.Add(group);
        }

        _regionGroups.Clear();
        _regionGroups.AddRange(retained);
        Revision = checked(Revision + 1);
        UpdatedAt = now;
    }

    private void SetInitialGroups(IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups)
    {
        ValidateShape(regionGroups);
        for (var index = 0; index < regionGroups.Count; index++)
        {
            var group = ShippingAuthoringRegionGroup.Create(DraftId, UuidV7.New(), index);
            group.Apply(index, regionGroups[index].Selections);
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
