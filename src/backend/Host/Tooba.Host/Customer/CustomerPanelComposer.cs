using Microsoft.EntityFrameworkCore;
using Tooba.AddressBook.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.CustomerProfile.Application;
using Tooba.Identity.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Wishlist.Application;

namespace Tooba.Host.Customer;

/// <summary>
/// خواندن پنل مشتری را روی Order و lookupهای مستقل Catalog/Party/Profile ترکیب می‌کند.
/// پرس‌وجوی بین‌schema، قیمت جاری Product و موجودی جاری در این read model وجود ندارد.
/// </summary>
public sealed class CustomerPanelComposer
{
    private readonly OrderDbContext _orders;
    private readonly CatalogDbContext _catalog;
    private readonly IPartyLookupGateway _parties;
    private readonly IPaymentDirectory _payments;
    private readonly IWishlistDirectory _wishlist;
    private readonly IAddressBookDirectory _addresses;
    private readonly ICustomerProfileDirectory _profiles;
    private readonly IIdentityContactLookup _identityContacts;

    /// <summary>
    /// ترکیب‌گر را با مرزهای خواندن مستقل می‌سازد.
    /// </summary>
    public CustomerPanelComposer(
        OrderDbContext orders,
        CatalogDbContext catalog,
        IPartyLookupGateway parties,
        IPaymentDirectory payments,
        IWishlistDirectory wishlist,
        IAddressBookDirectory addresses,
        ICustomerProfileDirectory profiles,
        IIdentityContactLookup identityContacts)
    {
        _orders = orders;
        _catalog = catalog;
        _parties = parties;
        _payments = payments;
        _wishlist = wishlist;
        _addresses = addresses;
        _profiles = profiles;
        _identityContacts = identityContacts;
    }

    /// <summary>
    /// داشبورد واقعی مشتری را برای User نشست می‌سازد؛ نمودار و شمارندهٔ جعلی ندارد.
    /// </summary>
    public async Task<CustomerDashboardPage> GetDashboardAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var groups = await LoadGroupsAsync(actorUserId, cancellationToken);
        var orders = new List<CustomerOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            orders.Add(await MapListItemAsync(group, actorUserId, cancellationToken));
        }

        var displayName = await ResolveDisplayNameAsync(actorUserId, groups, cancellationToken);
        var wishlistCount = await _wishlist.CountAsync(actorUserId, cancellationToken);
        var addressCount = await _addresses.CountAsync(actorUserId, cancellationToken);
        return new CustomerDashboardPage(
            actorUserId,
            displayName,
            orders.Count,
            orders.Count(x => !string.Equals(x.PaymentState, "Paid", StringComparison.Ordinal)),
            orders.Count(x => string.Equals(x.PaymentState, "Paid", StringComparison.Ordinal)),
            WishlistAvailable: true,
            WishlistCount: wishlistCount,
            AddressBookAvailable: true,
            AddressBookCount: addressCount,
            orders.Take(5).ToList());
    }

    /// <summary>
    /// پروفایل مشتری را از ماژول پروفایل و lookupهای Identity/Order ترکیب می‌کند.
    /// </summary>
    public async Task<CustomerProfilePage> GetProfileAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var latest = await LatestCheckoutAsync(actorUserId, cancellationToken);
        var stored = await _profiles.GetAsync(actorUserId, cancellationToken);
        var contact = await _identityContacts.GetContactAsync(actorUserId, cancellationToken);
        var address = FormatShippingAddress(latest);
        var displayName = stored?.DisplayName
            ?? (string.IsNullOrWhiteSpace(latest?.RecipientName) ? "مشتری توبا" : latest!.RecipientName);
        var mobile = contact.Mobile
            ?? (string.IsNullOrWhiteSpace(latest?.ContactMobile) ? null : latest!.ContactMobile);
        return new CustomerProfilePage(
            actorUserId,
            displayName,
            stored?.FirstName,
            stored?.LastName,
            contact.Email,
            mobile,
            stored?.BirthDate,
            stored?.Bio,
            address,
            EmailEditable: false,
            MobileEditable: false,
            AvatarUploadAvailable: false,
            NationalCodeEditable: false,
            AddressEditable: false,
            Editable: true);
    }

    /// <summary>
    /// فیلدهای توصیفی مجاز پروفایل Actor را ذخیره می‌کند.
    /// </summary>
    public async Task<CustomerProfilePage> UpdateProfileAsync(
        Guid actorUserId,
        CustomerProfileWrite input,
        CancellationToken cancellationToken)
    {
        await _profiles.UpsertAsync(actorUserId, input, cancellationToken);
        return await GetProfileAsync(actorUserId, cancellationToken);
    }

    /// <summary>
    /// فهرست سفارش‌ها را فقط با PlacedByUserId همان نشست برمی‌گرداند.
    /// </summary>
    public async Task<IReadOnlyList<CustomerOrderListItem>> ListOrdersAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var groups = await LoadGroupsAsync(actorUserId, cancellationToken);
        var orders = new List<CustomerOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            orders.Add(await MapListItemAsync(group, actorUserId, cancellationToken));
        }

        return orders;
    }

    /// <summary>
    /// جزئیات checkout را فقط در صورت مالکیت User نشست ترکیب می‌کند.
    /// </summary>
    public async Task<CustomerOrderDetailPage?> GetOrderAsync(
        Guid actorUserId,
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.CheckoutId == checkoutId && x.PlacedByUserId == actorUserId,
                cancellationToken);
        if (group is null)
        {
            return null;
        }

        var payment = await _payments.GetLatestForCheckoutAsync(
            group.CheckoutId,
            actorUserId,
            group.BuyerPartyId,
            cancellationToken);
        var paymentState = PaymentState(payment);

        var variantIds = group.SellerOrders
            .SelectMany(x => x.Lines)
            .Select(x => x.CatalogVariantId)
            .Distinct()
            .ToList();
        var variants = variantIds.Count == 0
            ? []
            : await _catalog.Variants.AsNoTracking()
                .Where(x => variantIds.Contains(x.VariantId))
                .Select(x => new { x.VariantId, x.ProductId })
                .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToList();
        var names = productIds.Count == 0
            ? []
            : await _catalog.LocalizedTexts.AsNoTracking()
                .Where(x =>
                    x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && productIds.Contains(x.OwnerId)
                    && x.FieldKey == "name")
                .ToListAsync(cancellationToken);
        var productNameMap = names
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .First().Value);
        var variantProductMap = variants.ToDictionary(x => x.VariantId, x => x.ProductId);

        var sellerViews = new List<CustomerSellerOrderView>();
        foreach (var sellerOrder in group.SellerOrders)
        {
            var party = await _parties.FindByIdAsync(sellerOrder.SellerPartyId, cancellationToken);
            var sellerName = party?.DisplayName ?? "فروشنده";
            var lineViews = sellerOrder.Lines.Select(line =>
            {
                variantProductMap.TryGetValue(line.CatalogVariantId, out var productId);
                productNameMap.TryGetValue(productId, out var title);
                return new CustomerOrderLineView(
                    line.OfferId,
                    string.IsNullOrWhiteSpace(title) ? "کالای سفارش" : title,
                    sellerName,
                    line.Quantity,
                    line.UnitPriceSnapshot,
                    line.LineTotalSnapshot + line.TaxAmountSnapshot - line.DiscountAmountSnapshot,
                    line.Currency);
            }).ToList();
            sellerViews.Add(new CustomerSellerOrderView(
                sellerOrder.SellerOrderId,
                sellerOrder.OrderNumber,
                sellerOrder.SellerPartyId,
                sellerName,
                sellerOrder.Status.ToString(),
                PaymentState(payment, sellerOrder.SellerOrderId),
                sellerOrder.GrandTotalSnapshot,
                sellerOrder.Currency,
                lineViews));
        }

        var listItem = MapListItem(group, paymentState);
        return new CustomerOrderDetailPage(
            group.CheckoutId,
            listItem.Reference,
            group.SubmittedAt,
            listItem.Status,
            listItem.PaymentState,
            group.SellerOrders.Sum(x => x.SubtotalSnapshot),
            group.SellerOrders.Sum(x => x.TaxSnapshot),
            group.SellerOrders.Sum(x => x.DiscountSnapshot),
            group.SellerOrders.Sum(x => x.GrandTotalSnapshot),
            group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            group.RecipientName,
            group.ContactMobile,
            group.ProvinceName,
            group.CityName,
            group.PostalAddress,
            group.PostalCode,
            group.ShippingMethodLabel,
            sellerViews);
    }

    private async Task<string> ResolveDisplayNameAsync(
        Guid actorUserId,
        IReadOnlyList<CheckoutGroup> groups,
        CancellationToken cancellationToken)
    {
        var stored = await _profiles.GetAsync(actorUserId, cancellationToken);
        if (stored is not null && !string.IsNullOrWhiteSpace(stored.DisplayName))
        {
            return stored.DisplayName;
        }

        return groups
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => x.RecipientName)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? "مشتری توبا";
    }

    private async Task<CheckoutGroup?> LatestCheckoutAsync(Guid actorUserId, CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Where(x => x.PlacedByUserId == actorUserId)
            .OrderByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static string? FormatShippingAddress(CheckoutGroup? latest)
    {
        if (latest is null)
        {
            return null;
        }

        var address = string.Join("، ", new[] { latest.ProvinceName, latest.CityName, latest.PostalAddress }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(address) ? null : address;
    }

    private async Task<IReadOnlyList<CheckoutGroup>> LoadGroupsAsync(
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => x.PlacedByUserId == actorUserId)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

    private async Task<CustomerOrderListItem> MapListItemAsync(
        CheckoutGroup group,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestForCheckoutAsync(
            group.CheckoutId,
            actorUserId,
            group.BuyerPartyId,
            cancellationToken);
        return MapListItem(group, PaymentState(payment));
    }

    private static CustomerOrderListItem MapListItem(CheckoutGroup group, string payment)
    {
        var orders = group.SellerOrders;
        var statuses = orders.Select(x => x.Status).Distinct().ToList();
        var status = statuses.Count == 1 ? statuses[0].ToString() : "Mixed";
        var references = orders.Select(x => x.OrderNumber).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return new CustomerOrderListItem(
            group.CheckoutId,
            references.Count == 0 ? group.CheckoutId.ToString("N")[..12] : string.Join(" / ", references),
            group.SubmittedAt,
            orders.Count,
            orders.Sum(x => x.Lines.Sum(line => line.Quantity)),
            orders.Sum(x => x.GrandTotalSnapshot),
            orders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            payment,
            status);
    }

    private static string PaymentState(PaymentSnapshot? payment) =>
        payment?.Status switch
        {
            PaymentStatus.Succeeded => "Paid",
            PaymentStatus.Failed or PaymentStatus.Cancelled => "Failed",
            _ => "PendingPayment",
        };

    private static string PaymentState(PaymentSnapshot? payment, Guid sellerOrderId) =>
        payment is null || payment.Allocations.Any(x => x.SellerOrderId == sellerOrderId)
            ? PaymentState(payment)
            : "PendingPayment";
}
