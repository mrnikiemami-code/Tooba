using MediatR;

namespace Tooba.AccessControl.Application.Queries.ListPermissionCatalog;

/// <summary>
/// پرس‌وجوی کاتالوگ کامل مجوزها برای پنل مدیر.
/// </summary>
public sealed record ListPermissionCatalogQuery : IRequest<IReadOnlyList<PermissionDefinition>>;

/// <summary>Handler کاتالوگ مجوز پلتفرم.</summary>
public sealed class ListPermissionCatalogQueryHandler : IRequestHandler<ListPermissionCatalogQuery, IReadOnlyList<PermissionDefinition>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی (مرجع canonical کاتالوگ).</param>
    public ListPermissionCatalogQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<PermissionDefinition>> Handle(ListPermissionCatalogQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_directory.ListCatalog());
}
