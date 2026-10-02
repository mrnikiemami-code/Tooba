using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Bootstrap.Commands;

/// <summary>
/// فرمان اطمینان از bootstrap دسترسی: نقش سیستمی پلتفرم را برای Actor پنل مدیر تضمین می‌کند.
/// </summary>
/// <param name="ActorUserId">شناسهٔ Actor مجاز پنل مدیر (از لایهٔ مجوز، معتبر).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
public sealed record EnsureAccessControlBootstrapCommand(
    Guid ActorUserId,
    string? TenantId) : IRequest<Result>;

/// <summary>Handler فرمان bootstrap دسترسی.</summary>
public sealed class EnsureAccessControlBootstrapHandler : IRequestHandler<EnsureAccessControlBootstrapCommand, Result>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public EnsureAccessControlBootstrapHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(EnsureAccessControlBootstrapCommand request, CancellationToken cancellationToken)
        => AccessControlOperation.ExecuteAsync(() => _directory.EnsureBootstrapAsync(
            request.ActorUserId,
            Array.Empty<Guid>(),
            request.TenantId,
            cancellationToken));
}
