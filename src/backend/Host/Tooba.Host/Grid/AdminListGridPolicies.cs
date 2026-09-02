using Tooba.Content.Application;
using Tooba.Fulfillment.Application;
using Tooba.Host.Admin;
using Tooba.Host.Reviews;
using Tooba.Returns.Application;
using Tooba.Settlement.Application;
using Tooba.Story.Application;

namespace Tooba.Host.Grid;

/// <summary>سیاست‌های GridQuery برای فهرست‌های Admin flat.</summary>
public static class AdminListGridPolicies
{
    /// <summary>گرید سفارش‌های Admin.</summary>
    public static readonly AdminListGridQueryPolicy<AdminOrderListItem> Orders = new(
    [
        new("reference", x => x.Reference, InMemoryGridFieldKind.Text, searchable: true),
        new("customer", x => x.CustomerDisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("sellers", x => x.SellerDisplayNames, InMemoryGridFieldKind.Text, searchable: true),
        new("lines", x => x.LineCount, InMemoryGridFieldKind.Number),
        new("payment", x => x.PaymentState, InMemoryGridFieldKind.Enum),
        new("status", x => x.Status, InMemoryGridFieldKind.Enum),
        new("amount", x => x.PayableAmount, InMemoryGridFieldKind.Number),
        new("created", x => x.SubmittedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "created",
        tieBreakerField: "reference");

    /// <summary>گرید فروشندگان Admin.</summary>
    public static readonly AdminListGridQueryPolicy<AdminSellerListItem> Sellers = new(
    [
        new("name", x => x.DisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("status", x => x.Status, InMemoryGridFieldKind.Enum),
        new("offers", x => x.ActiveOffers, InMemoryGridFieldKind.Number),
        new("orders", x => x.OrderCount, InMemoryGridFieldKind.Number),
    ],
        defaultSortField: "name",
        defaultSortDirection: "asc",
        tieBreakerField: "name");

    /// <summary>گرید مشتریان Admin.</summary>
    public static readonly AdminListGridQueryPolicy<AdminCustomerListItem> Customers = new(
    [
        new("name", x => x.DisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("contact", x => x.ContactMobile, InMemoryGridFieldKind.Text, searchable: true),
        new("orders", x => x.OrderCount, InMemoryGridFieldKind.Number),
        new("activity", x => x.LastOrderAt, InMemoryGridFieldKind.Date),
        new("status", x => x.Status, InMemoryGridFieldKind.Enum),
    ],
        defaultSortField: "activity",
        tieBreakerField: "name");

    /// <summary>گرید fulfillment Admin.</summary>
    public static readonly AdminListGridQueryPolicy<FulfillmentSnapshot> Fulfillments = new(
    [
        new("recipientName", x => x.RecipientName, InMemoryGridFieldKind.Text, searchable: true),
        new("fulfillmentId", x => x.FulfillmentId, InMemoryGridFieldKind.Text, searchable: true),
        new("checkoutId", x => x.CheckoutId, InMemoryGridFieldKind.Text, searchable: true),
        new("cityName", x => x.CityName, InMemoryGridFieldKind.Text, searchable: true),
        new("shipmentCount", x => x.Shipments.Count, InMemoryGridFieldKind.Number),
        new("status", x => x.Status.ToString(), InMemoryGridFieldKind.Enum),
    ],
        defaultSortField: "recipientName",
        defaultSortDirection: "asc",
        tieBreakerField: "fulfillmentId");

    /// <summary>گرید مرجوعی Admin.</summary>
    public static readonly AdminListGridQueryPolicy<ReturnSnapshot> Returns = new(
    [
        new("returnRequestId", x => x.ReturnRequestId, InMemoryGridFieldKind.Text, searchable: true),
        new("sellerOrderId", x => x.SellerOrderId, InMemoryGridFieldKind.Text, searchable: true),
        new("itemCount", x => x.Items.Count, InMemoryGridFieldKind.Number),
        new("refundAmount", x => x.RefundAmount, InMemoryGridFieldKind.Number),
        new("status", x => x.Status.ToString(), InMemoryGridFieldKind.Enum),
        new("createdAt", x => x.CreatedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "createdAt",
        tieBreakerField: "returnRequestId");

    /// <summary>گرید صف payout Admin.</summary>
    public static readonly AdminListGridQueryPolicy<PayoutRequestSnapshot> Payouts = new(
    [
        new("seller", x => x.SellerPartyId, InMemoryGridFieldKind.Text, searchable: true),
        new("amount", x => x.Amount, InMemoryGridFieldKind.Number),
        new("status", x => x.Status.ToString(), InMemoryGridFieldKind.Enum),
        new("created", x => x.CreatedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "created",
        tieBreakerField: "seller");

    /// <summary>گرید دریافت‌های Admin (پرداخت مشتری).</summary>
    public static readonly AdminListGridQueryPolicy<AdminReceiptListItem> Payments = new(
    [
        new("reference", x => x.OrderReference, InMemoryGridFieldKind.Text, searchable: true),
        new("customer", x => x.CustomerDisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("amount", x => x.Amount, InMemoryGridFieldKind.Number),
        new("status", x => x.Status, InMemoryGridFieldKind.Enum),
        new("provider", x => x.ProviderCode, InMemoryGridFieldKind.Text, searchable: true),
        new("created", x => x.CreatedAt, InMemoryGridFieldKind.Date),
        new("completed", x => x.CompletedAt ?? x.CreatedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "created",
        tieBreakerField: "reference");

    /// <summary>گرید مقالات Admin.</summary>
    public static readonly AdminListGridQueryPolicy<AdminArticleSnapshot> Content = new(
    [
        new("title", x => x.Title, InMemoryGridFieldKind.Text, searchable: true),
        new("slug", x => x.Slug, InMemoryGridFieldKind.Text, searchable: true),
        new("status", x => x.Status.ToString(), InMemoryGridFieldKind.Enum),
        new("category", x => x.Category, InMemoryGridFieldKind.Text, searchable: true),
        new("locale", x => x.Locale, InMemoryGridFieldKind.Text, searchable: true),
        new("authorDisplayName", x => x.AuthorDisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("updated", x => x.UpdatedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "updated",
        tieBreakerField: "title");

    /// <summary>گرید نویسندگان Admin.</summary>
    public static readonly AdminListGridQueryPolicy<ContentAuthorGridRowDto> ContentAuthors = new(
    [
        new("displayName", x => x.DisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("slug", x => x.Slug, InMemoryGridFieldKind.Text, searchable: true),
        new("isActive", x => x.IsActive, InMemoryGridFieldKind.Enum),
        new("updated", x => x.UpdatedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "updated",
        tieBreakerField: "displayName");

    /// <summary>گرید نظرات Admin.</summary>
    public static readonly AdminListGridQueryPolicy<AdminReviewItem> Reviews = new(
    [
        new("reviewer", x => x.AuthorDisplayName, InMemoryGridFieldKind.Text, searchable: true),
        new("product", x => x.ProductTitle, InMemoryGridFieldKind.Text, searchable: true),
        new("rating", x => x.Rating, InMemoryGridFieldKind.Number),
        new("excerpt", x => x.Body, InMemoryGridFieldKind.Text, searchable: true),
        new("verified", x => x.VerifiedPurchase, InMemoryGridFieldKind.Enum),
        new("status", x => x.Status, InMemoryGridFieldKind.Enum),
        new("created", x => x.CreatedAt, InMemoryGridFieldKind.Date),
    ],
        defaultSortField: "created",
        tieBreakerField: "reviewer");

    /// <summary>گرید استوری Admin.</summary>
    public static readonly AdminListGridQueryPolicy<AdminStorySnapshot> Stories = new(
    [
        new("title", x => x.Title, InMemoryGridFieldKind.Text, searchable: true),
        new("status", x => x.Status.ToString(), InMemoryGridFieldKind.Enum),
        new("reviewStatus", x => x.ReviewStatus.ToString(), InMemoryGridFieldKind.Enum),
        new("origin", x => x.Origin.ToString(), InMemoryGridFieldKind.Enum),
        new("locale", x => x.Locale, InMemoryGridFieldKind.Text),
        new("market", x => x.Market, InMemoryGridFieldKind.Text),
        new("displayOrder", x => x.DisplayOrder, InMemoryGridFieldKind.Number),
        new("items", x => x.Items.Count, InMemoryGridFieldKind.Number),
    ],
        defaultSortField: "displayOrder",
        defaultSortDirection: "asc",
        tieBreakerField: "title");
}
