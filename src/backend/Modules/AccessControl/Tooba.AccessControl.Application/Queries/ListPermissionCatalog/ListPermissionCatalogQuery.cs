using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Application.Permissions;

namespace Tooba.AccessControl.Application.Queries.ListPermissionCatalog;

/// <summary>
/// پرس‌وجوی کاتالوگ کامل مجوزها برای پنل مدیر.
/// </summary>
public sealed record ListPermissionCatalogQuery : IRequest<Result<IReadOnlyList<PermissionDefinition>>>;

/// <summary>Handler کاتالوگ مجوز پلتفرم.</summary>
public sealed class ListPermissionCatalogQueryHandler
    : IRequestHandler<ListPermissionCatalogQuery, Result<IReadOnlyList<PermissionDefinition>>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی (مرجع canonical کاتالوگ).</param>
    public ListPermissionCatalogQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PermissionDefinition>>> Handle(
        ListPermissionCatalogQuery request, CancellationToken cancellationToken)
        => AccessControlOperation.ExecuteAsync(() => Task.FromResult(_directory.ListCatalog()));
}
