namespace Tooba.Support.Domain.ValueObjects;

/// <summary>وضعیت چرخهٔ تیکت پشتیبانی.</summary>
public enum TicketStatus
{
    /// <summary>باز و در صف بررسی.</summary>
    Open = 0,
    /// <summary>در حال بررسی توسط اپراتور.</summary>
    InProgress = 1,
    /// <summary>در انتظار پاسخ مشتری.</summary>
    WaitingForCustomer = 2,
    /// <summary>در انتظار پاسخ فروشنده.</summary>
    WaitingForSeller = 3,
    /// <summary>حل‌شده.</summary>
    Resolved = 4,
    /// <summary>بسته‌شده.</summary>
    Closed = 5,
}

/// <summary>اولویت تیکت.</summary>
public enum TicketPriority
{
    /// <summary>پایین.</summary>
    Low = 0,
    /// <summary>عادی.</summary>
    Normal = 1,
    /// <summary>بالا.</summary>
    High = 2,
}

/// <summary>دستهٔ موضوع تیکت.</summary>
public enum TicketCategory
{
    /// <summary>سفارش.</summary>
    Order = 0,
    /// <summary>پرداخت.</summary>
    Payment = 1,
    /// <summary>مرجوعی.</summary>
    Return = 2,
    /// <summary>محصول.</summary>
    Product = 3,
    /// <summary>سایر.</summary>
    Other = 4,
}

/// <summary>نقش درخواست‌کنندهٔ تیکت.</summary>
public enum RequesterKind
{
    /// <summary>مشتری.</summary>
    Customer = 0,
    /// <summary>فروشنده.</summary>
    Seller = 1,
    /// <summary>مدیر.</summary>
    Admin = 2,
}

/// <summary>نقش نویسندهٔ پیام.</summary>
public enum AuthorKind
{
    /// <summary>مشتری.</summary>
    Customer = 0,
    /// <summary>فروشنده.</summary>
    Seller = 1,
    /// <summary>مدیر.</summary>
    Admin = 2,
    /// <summary>سیستم.</summary>
    System = 3,
}
