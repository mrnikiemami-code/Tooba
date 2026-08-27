namespace Tooba.Host.AccessControl;

/// <summary>
/// شناسه‌های قطعی سناریوی دمو ACC پس از seed Development (بدون رمز).
/// </summary>
internal static class AccessControlDemoSnapshot
{
    private static readonly object Gate = new();
    private static AccessControlDemoContext? _current;

    /// <summary>آخرین snapshot سناریوی دمو.</summary>
    public static AccessControlDemoContext? Current
    {
        get
        {
            lock (Gate)
            {
                return _current;
            }
        }
    }

    internal static void Publish(AccessControlDemoContext context)
    {
        lock (Gate)
        {
            _current = context;
        }
    }
}

/// <summary>زمینهٔ preview کنترل دسترسی برای UI و capture.</summary>
internal sealed record AccessControlDemoContext(
    Guid PlatformAdminActorId,
    Guid SellerPartyId,
    string SellerDisplayName,
    Guid SellerOwnerActorId,
    string SellerOwnerLabel,
    Guid EmployeeActorId,
    string EmployeeLabel,
    Guid MobileCategoryId,
    string MobileCategoryName,
    Guid BooksCategoryId,
    string BooksCategoryName,
    Guid MobileOfferId,
    Guid BooksOfferId,
    Guid MobileSellerOrderId,
    string MobileOrderNumber,
    Guid BooksSellerOrderId,
    string BooksOrderNumber,
    Guid MixedSellerOrderId,
    string MixedOrderNumber,
    Guid MobileOrderOperatorRoleId,
    string MobileOrderOperatorRoleCode);
