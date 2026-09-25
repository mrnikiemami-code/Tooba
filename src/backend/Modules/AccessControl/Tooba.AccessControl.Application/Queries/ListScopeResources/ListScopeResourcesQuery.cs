using MediatR;
using Tooba.Catalog.Contracts;

namespace Tooba.AccessControl.Application.Queries.ListScopeResources;

/// <summary>
/// گونهٔ منبع scope قابل انتخاب در مرکز کنترل دسترسی.
/// </summary>
public enum AccessScopeResourceKind
{
    /// <summary>دسته.</summary>
    Category = 0,

    /// <summary>برند.</summary>
    Brand = 1,

    /// <summary>محصول.</summary>
    Product = 2,

    /// <summary>انبار (به تعویق افتاده).</summary>
    Warehouse = 3,

    /// <summary>فروشگاه (به تعویق افتاده).</summary>
    Store = 4,

    /// <summary>بخش سفارش (به تعویق افتاده).</summary>
    OrderSegment = 5,
}

/// <summary>
/// نتیجهٔ فهرست منابع scope — پاسخ بی‌طرف برای Endpoints.
/// </summary>
/// <param name="Deferred">آیا این منبع به تعویق افتاده است.</param>
/// <param name="Items">اقلام؛ در حالت deferred خالی است.</param>
public sealed record ScopeResourceListResult(
    bool Deferred,
    IReadOnlyList<object> Items);

/// <summary>
/// پرس‌وجوی منابع scope برای انتخابگر Access Control.
/// </summary>
/// <param name="Kind">گونهٔ منبع درخواستی.</param>
/// <param name="Search">عبارت جستجو در صورت وجود.</param>
public sealed record ListScopeResourcesQuery(
    AccessScopeResourceKind Kind,
    string? Search) : IRequest<ScopeResourceListResult>;

/// <summary>
/// Handler پرس‌وجوی منابع scope — منابع مشخص از درز قراردادی Catalog و منابع دیگر به‌صورت deferred.
/// </summary>
public sealed class ListScopeResourcesQueryHandler
    : IRequestHandler<ListScopeResourcesQuery, ScopeResourceListResult>
{
    private readonly IAccessControlScopeResourceLookup _catalog;

    /// <summary>سازنده.</summary>
    /// <param name="catalog">درز قراردادی Catalog.</param>
    public ListScopeResourcesQueryHandler(IAccessControlScopeResourceLookup catalog) => _catalog = catalog;

    /// <inheritdoc />
    public async Task<ScopeResourceListResult> Handle(
        ListScopeResourcesQuery request, CancellationToken cancellationToken)
    {
        switch (request.Kind)
        {
            case AccessScopeResourceKind.Category:
                var categories = await _catalog.ListCategoriesAsync(request.Search, cancellationToken);
                return new ScopeResourceListResult(false, categories.Cast<object>().ToList());

            case AccessScopeResourceKind.Brand:
                var brands = await _catalog.ListBrandsAsync(request.Search, cancellationToken);
                return new ScopeResourceListResult(false, brands.Cast<object>().ToList());

            case AccessScopeResourceKind.Product:
                var products = await _catalog.ListProductsAsync(request.Search, cancellationToken);
                return new ScopeResourceListResult(false, products.Cast<object>().ToList());

            default:
                return new ScopeResourceListResult(true, Array.Empty<object>());
        }
    }
}
