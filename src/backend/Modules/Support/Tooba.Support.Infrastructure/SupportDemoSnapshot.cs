namespace Tooba.Support.Infrastructure;

/// <summary>شناسه‌های پایدار دانهٔ توسعه برای demo-preview.</summary>
public static class SupportDemoIds
{
    /// <summary>تیکت باز مشتری.</summary>
    public static readonly Guid CustomerOpenTicketId = Guid.Parse("01900000-0000-7000-8000-000000000001");

    /// <summary>تیکت حل‌شده مشتری.</summary>
    public static readonly Guid CustomerResolvedTicketId = Guid.Parse("01900000-0000-7000-8000-000000000002");

    /// <summary>تیکت در انتظار فروشنده.</summary>
    public static readonly Guid SellerWaitingTicketId = Guid.Parse("01900000-0000-7000-8000-000000000003");

    /// <summary>تیکت باز فروشنده.</summary>
    public static readonly Guid SellerOpenTicketId = Guid.Parse("01900000-0000-7000-8000-000000000004");

    /// <summary>پیام اول مشتری روی تیکت باز.</summary>
    public static readonly Guid CustomerOpenFirstMessageId = Guid.Parse("01900000-0000-7000-8000-000000000011");

    /// <summary>پاسخ عمومی Admin روی تیکت باز مشتری.</summary>
    public static readonly Guid CustomerOpenAdminReplyId = Guid.Parse("01900000-0000-7000-8000-000000000012");

    /// <summary>پیام اول مشتری روی تیکت حل‌شده.</summary>
    public static readonly Guid CustomerResolvedFirstMessageId = Guid.Parse("01900000-0000-7000-8000-000000000021");

    /// <summary>پیام اول فروشنده روی تیکت waiting.</summary>
    public static readonly Guid SellerWaitingFirstMessageId = Guid.Parse("01900000-0000-7000-8000-000000000031");

    /// <summary>پیام اول فروشنده روی تیکت باز.</summary>
    public static readonly Guid SellerOpenFirstMessageId = Guid.Parse("01900000-0000-7000-8000-000000000041");

    /// <summary>شناسهٔ نمایشی سفارش مرتبط (soft؛ بدون JOIN).</summary>
    public static readonly Guid DemoRelatedOrderId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0001");
}

/// <summary>snapshot شناسه‌های demo برای USER-PREVIEW.</summary>
public sealed record SupportDemoSnapshot(
    Guid CustomerOpenTicketId,
    Guid CustomerResolvedTicketId,
    Guid SellerWaitingTicketId,
    Guid SellerOpenTicketId,
    Guid? CustomerActorUserId,
    Guid? SellerPartyId,
    Guid? SellerActorUserId,
    Guid? AdminActorUserId,
    Guid? RelatedOrderId,
    string Note);

/// <summary>نگهدارندهٔ snapshot پس از دانهٔ توسعه.</summary>
public static class SupportDemoSnapshotStore
{
    private static SupportDemoSnapshot? _current;

    /// <summary>آخرین snapshot دانه‌شده؛ null اگر هنوز آماده نباشد.</summary>
    public static SupportDemoSnapshot? Current => _current;

    /// <summary>snapshot را منتشر می‌کند.</summary>
    public static void Publish(SupportDemoSnapshot snapshot) => _current = snapshot;
}
