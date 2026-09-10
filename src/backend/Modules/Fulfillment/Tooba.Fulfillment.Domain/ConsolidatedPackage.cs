using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain;

/// <summary>
/// وضعیت بسته تجمیعی چندفروشنده‌ای. لایهٔ ارکستراسیون است، نه جایگزین Shipment.
/// </summary>
public enum ConsolidatedPackageStatus
{
    /// <summary>ایجاد شده؛ قابل ابطال و ارسال مرکزی.</summary>
    Created = 0,

    /// <summary>تمام اعضای عضو ارسال شده‌اند.</summary>
    Dispatched = 1,

    /// <summary>تمام اعضای عضو تحویل شده‌اند.</summary>
    Delivered = 2,

    /// <summary>ابطال پیش از ارسال؛ عضویت آزاد شده.</summary>
    Cancelled = 3,
}

/// <summary>
/// عضو بسته تجمیعی (مرسولهٔ فروشنده).
/// </summary>
public sealed class ConsolidatedPackageMember
{
    private ConsolidatedPackageMember()
    {
    }

    /// <summary>شناسه عضویت.</summary>
    public Guid ConsolidatedPackageMemberId { get; init; }

    /// <summary>بسته مالک.</summary>
    public Guid ConsolidatedPackageId { get; init; }

    /// <summary>مرسوله عضو.</summary>
    public Guid ShipmentId { get; init; }

    /// <summary>فروشندهٔ مرسوله در زمان عضویت.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>fulfillment مالک مرسوله.</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>زمان عضویت.</summary>
    public DateTimeOffset JoinedAt { get; init; }

    /// <summary>
    /// زمان آزادسازی عضویت (پس از Cancel بسته). null یعنی عضویت فعال برای ایندکس یکتایی.
    /// </summary>
    public DateTimeOffset? ReleasedAt { get; private set; }

    internal static ConsolidatedPackageMember Create(
        Guid packageId,
        Guid shipmentId,
        Guid sellerPartyId,
        Guid fulfillmentId,
        DateTimeOffset now) =>
        new()
        {
            ConsolidatedPackageMemberId = UuidV7.New(),
            ConsolidatedPackageId = packageId,
            ShipmentId = shipmentId,
            SellerPartyId = sellerPartyId,
            FulfillmentId = fulfillmentId,
            JoinedAt = now,
        };

    internal void Release(DateTimeOffset now)
    {
        ReleasedAt ??= now;
    }

    /// <summary>آیا عضویت هنوز برای قفل/ایندکس فعال است.</summary>
    public bool IsActiveMembership => ReleasedAt is null;
}

/// <summary>
/// بسته تجمیعی مرکزی روی مرسوله‌های چند فروشنده از یک Checkout.
/// </summary>
public sealed class ConsolidatedPackage
{
    private readonly List<ConsolidatedPackageMember> _members = [];

    private ConsolidatedPackage()
    {
    }

    /// <summary>شناسه بسته.</summary>
    public Guid ConsolidatedPackageId { get; init; }

    /// <summary>شماره انسانی MP-…</summary>
    public string PackageNumber { get; init; } = string.Empty;

    /// <summary>checkout / سفارش مرجع.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>وضعیت بسته.</summary>
    public ConsolidatedPackageStatus Status { get; private set; }

    /// <summary>کد روش ارسال مرکزی.</summary>
    public string ShippingMethodCode { get; private set; } = string.Empty;

    /// <summary>برچسب روش ارسال مرکزی.</summary>
    public string ShippingMethodLabel { get; private set; } = string.Empty;

    /// <summary>کد رهگیری مرکزی (مشتری‌محور).</summary>
    public string? TrackingReference { get; private set; }

    /// <summary>یادداشت اختیاری.</summary>
    public string? Note { get; private set; }

    /// <summary>ایجادکننده.</summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>زمان ارسال مرکزی.</summary>
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>زمان تحویل مرکزی.</summary>
    public DateTimeOffset? DeliveredAt { get; private set; }

    /// <summary>زمان ابطال.</summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>اعضا.</summary>
    public IReadOnlyList<ConsolidatedPackageMember> Members => _members;

    /// <summary>آیا بسته هنوز عضو را قفل می‌کند (Created یا Dispatched).</summary>
    public bool LocksMembers =>
        Status is ConsolidatedPackageStatus.Created or ConsolidatedPackageStatus.Dispatched;

    /// <summary>آیا بسته از نظر یکتایی عضویت فعال است (غیر Cancelled).</summary>
    public bool IsActiveForMembership => Status != ConsolidatedPackageStatus.Cancelled;

    /// <summary>
    /// بسته تجمیعی می‌سازد. حداقل دو فروشندهٔ متمایز الزامی است.
    /// </summary>
    public static ConsolidatedPackage Create(
        Guid checkoutId,
        IReadOnlyList<(Guid ShipmentId, Guid SellerPartyId, Guid FulfillmentId)> members,
        string shippingMethodCode,
        string shippingMethodLabel,
        string? trackingReference,
        string? note,
        Guid? createdBy,
        DateTimeOffset now)
    {
        if (checkoutId == Guid.Empty)
        {
            throw new InvalidOperationException("fulfillment.package.checkout_required");
        }

        if (members is null || members.Count < 2)
        {
            throw new InvalidOperationException("fulfillment.package.requires_multi_seller");
        }

        var distinctShipments = members.Select(x => x.ShipmentId).Distinct().ToArray();
        if (distinctShipments.Length != members.Count)
        {
            throw new InvalidOperationException("fulfillment.package.duplicate_shipment");
        }

        var distinctSellers = members.Select(x => x.SellerPartyId).Distinct().ToArray();
        if (distinctSellers.Length < 2)
        {
            throw new InvalidOperationException("fulfillment.package.requires_multi_seller");
        }

        var methodCode = (shippingMethodCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            throw new InvalidOperationException("fulfillment.package.shipping_method_required");
        }

        var packageId = UuidV7.New();
        var package = new ConsolidatedPackage
        {
            ConsolidatedPackageId = packageId,
            PackageNumber = BuildPackageNumber(packageId),
            CheckoutId = checkoutId,
            Status = ConsolidatedPackageStatus.Created,
            ShippingMethodCode = methodCode,
            ShippingMethodLabel = string.IsNullOrWhiteSpace(shippingMethodLabel)
                ? methodCode
                : shippingMethodLabel.Trim(),
            TrackingReference = string.IsNullOrWhiteSpace(trackingReference)
                ? null
                : trackingReference.Trim(),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var member in members)
        {
            package._members.Add(ConsolidatedPackageMember.Create(
                packageId,
                member.ShipmentId,
                member.SellerPartyId,
                member.FulfillmentId,
                now));
        }

        return package;
    }

    /// <summary>اعضای بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedMembers(IEnumerable<ConsolidatedPackageMember> members)
    {
        _members.Clear();
        _members.AddRange(members);
    }

    /// <summary>ابطال فقط پیش از Dispatch؛ عضویت آزاد می‌شود.</summary>
    public void Cancel(DateTimeOffset now)
    {
        if (Status == ConsolidatedPackageStatus.Cancelled)
        {
            return;
        }

        if (Status != ConsolidatedPackageStatus.Created)
        {
            throw new InvalidOperationException("fulfillment.package.cancel_after_dispatch");
        }

        foreach (var member in _members.Where(x => x.IsActiveMembership))
        {
            member.Release(now);
        }

        Status = ConsolidatedPackageStatus.Cancelled;
        CancelledAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// بسته را Dispatched می‌کند فقط وقتی همهٔ اعضای فعال در وضعیت ارسال/تحویل هستند.
    /// </summary>
    public void MarkDispatched(DateTimeOffset now, IReadOnlyDictionary<Guid, ShipmentStatus> memberStatuses)
    {
        if (Status == ConsolidatedPackageStatus.Dispatched || Status == ConsolidatedPackageStatus.Delivered)
        {
            return;
        }

        if (Status != ConsolidatedPackageStatus.Created)
        {
            throw new InvalidOperationException("fulfillment.package.dispatch_invalid_state");
        }

        EnsureAllMembersReadyForDispatch(memberStatuses);
        Status = ConsolidatedPackageStatus.Dispatched;
        DispatchedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// بسته را Delivered می‌کند فقط وقتی همهٔ اعضای فعال Delivered هستند.
    /// </summary>
    public void MarkDelivered(DateTimeOffset now, IReadOnlyDictionary<Guid, ShipmentStatus> memberStatuses)
    {
        if (Status == ConsolidatedPackageStatus.Delivered)
        {
            return;
        }

        if (Status != ConsolidatedPackageStatus.Dispatched)
        {
            throw new InvalidOperationException("fulfillment.package.deliver_before_dispatch");
        }

        EnsureAllMembersDelivered(memberStatuses);
        Status = ConsolidatedPackageStatus.Delivered;
        DeliveredAt = now;
        UpdatedAt = now;
    }

    /// <summary>کد رهگیری مرکزی را در صورت خالی بودن تنظیم می‌کند.</summary>
    public void AssignTrackingIfMissing(string? trackingReference, DateTimeOffset now)
    {
        if (Status is ConsolidatedPackageStatus.Cancelled or ConsolidatedPackageStatus.Delivered)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(TrackingReference) || string.IsNullOrWhiteSpace(trackingReference))
        {
            return;
        }

        TrackingReference = trackingReference.Trim();
        UpdatedAt = now;
    }

    private void EnsureAllMembersReadyForDispatch(IReadOnlyDictionary<Guid, ShipmentStatus> memberStatuses)
    {
        foreach (var member in _members.Where(x => x.IsActiveMembership))
        {
            if (!memberStatuses.TryGetValue(member.ShipmentId, out var status))
            {
                throw new InvalidOperationException("fulfillment.package.member_state_changed");
            }

            if (status is not (ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered))
            {
                throw new InvalidOperationException("fulfillment.package.member_state_changed");
            }
        }
    }

    private void EnsureAllMembersDelivered(IReadOnlyDictionary<Guid, ShipmentStatus> memberStatuses)
    {
        foreach (var member in _members.Where(x => x.IsActiveMembership))
        {
            if (!memberStatuses.TryGetValue(member.ShipmentId, out var status)
                || status != ShipmentStatus.Delivered)
            {
                throw new InvalidOperationException("fulfillment.package.member_state_changed");
            }
        }
    }

    private static string BuildPackageNumber(Guid packageId)
    {
        var hex = packageId.ToString("N");
        return $"MP-{hex[..12].ToUpperInvariant()}";
    }
}
