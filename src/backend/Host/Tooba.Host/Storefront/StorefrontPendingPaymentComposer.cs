using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Host.Storefront;

/// <summary>
/// تصویر دسته‌ای در انتظار پرداخت. مالکیت مهمان فقط اثبات متعهد است؛ سبد فعال منبع اختیار نیست.
/// </summary>
public sealed class StorefrontPendingPaymentComposer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    private readonly OrderDbContext _orders;
    private readonly PaymentDbContext _payments;
    private readonly CatalogDbContext _catalog;
    private readonly StorefrontCartComposer _carts;
    private readonly IReservationCycleDirectory _cycles;
    private readonly CurrentAuthenticatedSession _session;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _http;

    /// <summary>ترکیب Host برای فهرست در انتظار پرداخت.</summary>
    internal StorefrontPendingPaymentComposer(
        OrderDbContext orders,
        PaymentDbContext payments,
        CatalogDbContext catalog,
        StorefrontCartComposer carts,
        IReservationCycleDirectory cycles,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IHttpContextAccessor http)
    {
        _orders = orders;
        _payments = payments;
        _catalog = catalog;
        _carts = carts;
        _cycles = cycles;
        _session = session;
        _environment = environment;
        _http = http;
    }

    /// <summary>فهرست سفارش‌های unpaid قابل‌اقدام یا اطلاع‌رسانی را برمی‌گرداند.</summary>
    public async Task<StorefrontPendingPaymentPage> ListAsync(
        StorefrontPendingPaymentQueryRequest? query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var groups = await ResolveOwnedGroupsAsync(query, cancellationToken);
        if (groups.Count == 0)
        {
            return new StorefrontPendingPaymentPage(now, []);
        }

        var checkoutIds = groups.Select(x => x.CheckoutId).ToArray();
        var paymentRows = await _payments.Payments.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var latestPaymentRows = paymentRows
            .GroupBy(x => x.CheckoutId)
            .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
            .ToList();
        var latestPaymentIds = latestPaymentRows.Select(x => x.PaymentId).ToArray();
        var evidenceByPayment = latestPaymentIds.Length == 0
            ? new Dictionary<Guid, DateTimeOffset?>()
            : (await _payments.Attempts.AsNoTracking()
                .Where(x => latestPaymentIds.Contains(x.PaymentId))
                .Select(x => new { x.PaymentId, x.EvidenceSubmittedAt, x.CreatedAt })
                .ToListAsync(cancellationToken))
                .GroupBy(x => x.PaymentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).First().EvidenceSubmittedAt);
        var latestPayments = latestPaymentRows.ToDictionary(
            row => row.CheckoutId,
            row => new StorefrontPendingPaymentProjector.PaymentInput(
                row.PaymentId,
                row.Status,
                row.ProviderCode,
                evidenceByPayment.GetValueOrDefault(row.PaymentId),
                row.Amount,
                row.Currency));
        var cycleMap = await _cycles.GetProjectionsAsync(checkoutIds, now, null, cancellationToken);
        var lineCatalog = await LoadLineCatalogAsync(groups, cancellationToken);
        var inputs = groups.Select(group => new StorefrontPendingPaymentProjector.CheckoutInput(
            group.CheckoutId,
            group.CartId,
            group.PlacedByUserId,
            group.SubmittedAt,
            group.SellerOrders.Select(order => new StorefrontPendingPaymentProjector.SellerInput(
                order.OrderNumber,
                order.Status,
                order.GrandTotalSnapshot,
                order.Currency,
                order.Lines.Select(line =>
                {
                    lineCatalog.TryGetValue(line.CatalogVariantId, out var catalog);
                    return new StorefrontPendingPaymentProjector.LineInput(
                        catalog.Title,
                        line.Quantity,
                        catalog.MediaAssetId);
                }).ToArray())).ToArray())).ToArray();
        return StorefrontPendingPaymentProjector.Project(inputs, latestPayments, cycleMap, now);
    }

    private async Task<IReadOnlyList<CheckoutGroup>> ResolveOwnedGroupsAsync(
        StorefrontPendingPaymentQueryRequest? query,
        CancellationToken cancellationToken)
    {
        var actor = ResolveListActor();
        if (actor is Guid userId)
        {
            return await _orders.Checkouts.AsNoTracking()
                .Include(x => x.SellerOrders)
                .ThenInclude(x => x.Lines)
                .Where(x => x.PlacedByUserId == userId)
                .OrderByDescending(x => x.SubmittedAt)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        var proofs = (query?.Proofs ?? [])
            .Where(x => x.CheckoutId != Guid.Empty && x.CartId != Guid.Empty)
            .Take(20)
            .ToArray();
        if (proofs.Length == 0)
        {
            return [];
        }

        var ids = proofs.Select(x => x.CheckoutId).Distinct().ToArray();
        var groups = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var allowed = new List<CheckoutGroup>();
        foreach (var group in groups)
        {
            var proof = proofs.FirstOrDefault(x => x.CheckoutId == group.CheckoutId);
            if (proof is null || proof.CartId != group.CartId)
            {
                continue;
            }

            var owned = await _carts.TryGetForOwnershipAsync(group.CartId, proof.GuestSecret, cancellationToken);
            if (owned is null)
            {
                continue;
            }

            allowed.Add(group);
        }

        return allowed;
    }

    private Guid? ResolveListActor()
    {
        if (_session.IsAuthenticated && _session.UserId is Guid userId && userId != Guid.Empty)
        {
            return userId;
        }

        var request = _http.HttpContext?.Request;
        var isDevSeam = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");
        if (isDevSeam
            && request is not null
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var headerActor)
            && headerActor != Guid.Empty)
        {
            return headerActor;
        }

        return null;
    }

    private async Task<Dictionary<Guid, (string Title, Guid? MediaAssetId)>> LoadLineCatalogAsync(
        IReadOnlyList<CheckoutGroup> groups,
        CancellationToken cancellationToken)
    {
        var variantIds = groups
            .SelectMany(x => x.SellerOrders.SelectMany(o => o.Lines.Select(l => l.CatalogVariantId)))
            .Distinct()
            .ToArray();
        var result = new Dictionary<Guid, (string Title, Guid? MediaAssetId)>();
        if (variantIds.Length == 0)
        {
            return result;
        }

        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var names = productIds.Length == 0
            ? []
            : await _catalog.LocalizedTexts.AsNoTracking()
                .Where(x =>
                    x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && productIds.Contains(x.OwnerId)
                    && x.FieldKey == "name")
                .ToListAsync(cancellationToken);
        var productName = names
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .First().Value);
        var media = productIds.Length == 0
            ? []
            : await _catalog.MediaReferences.AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => new { x.ProductId, x.MediaAssetId })
                .ToListAsync(cancellationToken);
        var productMedia = media
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (Guid?)g.First().MediaAssetId);
        var variantProduct = variants.ToDictionary(x => x.VariantId, x => x.ProductId);
        foreach (var variantId in variantIds)
        {
            variantProduct.TryGetValue(variantId, out var productId);
            productName.TryGetValue(productId, out var title);
            productMedia.TryGetValue(productId, out var asset);
            result[variantId] = (string.IsNullOrWhiteSpace(title) ? "کالا" : title, asset);
        }

        return result;
    }
}
