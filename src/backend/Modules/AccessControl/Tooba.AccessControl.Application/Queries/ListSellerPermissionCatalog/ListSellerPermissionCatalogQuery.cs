using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Queries.ListSellerPermissionCatalog;

/// <summary>
/// یک ردیف کاتالوگ مجوز از دید فروشنده، با وضعیت سقف.
/// </summary>
/// <param name="PermissionId">شناسهٔ پایدار مجوز.</param>
/// <param name="Module">ماژول.</param>
/// <param name="DisplayNameKey">کلید نمایش.</param>
/// <param name="DescriptionKey">کلید توضیح.</param>
/// <param name="Delegable">قابل تفویض.</param>
/// <param name="ScopeKinds">گونه‌های scope.</param>
/// <param name="DisabledByCeiling">توسط سقف غیرفعال شده.</param>
/// <param name="PlatformOnly">فقط پلتفرم.</param>
public sealed record SellerPermissionCatalogItem(
    string PermissionId,
    string Module,
    string DisplayNameKey,
    string DescriptionKey,
    bool Delegable,
    IReadOnlyList<AccessScopeKind> ScopeKinds,
    bool DisabledByCeiling,
    bool PlatformOnly);

/// <summary>
/// پرس‌وجوی کاتالوگ مجوز از دید فروشنده با اعمال سقف.
/// </summary>
/// <param name="SellerPartyId">شناسهٔ فروشندهٔ مجاز.</param>
public sealed record ListSellerPermissionCatalogQuery(Guid SellerPartyId) : IRequest<IReadOnlyList<SellerPermissionCatalogItem>>;

/// <summary>Handler کاتالوگ مجوز فروشنده.</summary>
public sealed class ListSellerPermissionCatalogQueryHandler : IRequestHandler<ListSellerPermissionCatalogQuery, IReadOnlyList<SellerPermissionCatalogItem>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public ListSellerPermissionCatalogQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerPermissionCatalogItem>> Handle(
        ListSellerPermissionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var ceiling = await _directory.GetSellerCeilingAsync(request.SellerPartyId, cancellationToken);
        return _directory.ListCatalog()
            .Select(p => new SellerPermissionCatalogItem(
                p.PermissionId,
                p.Module,
                p.DisplayNameKey,
                p.DescriptionKey,
                p.Delegable,
                p.ScopeKinds,
                DisabledByCeiling: p.Delegable && ceiling.All(c => c.PermissionId != p.PermissionId || !c.Enabled),
                PlatformOnly: !p.Delegable))
            .ToArray();
    }
}
