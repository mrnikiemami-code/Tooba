namespace Tooba.Order.Application.Admin.Customers.Models;

/// <summary>
/// مشتری صادقانهٔ عملیاتی بر پایهٔ User سفارش و آخرین snapshot گیرنده.
/// </summary>
public sealed record AdminCustomerListItem(
    Guid CustomerUserId,
    string DisplayName,
    string? ContactMobile,
    int OrderCount,
    DateTimeOffset LastOrderAt,
    string Status);
