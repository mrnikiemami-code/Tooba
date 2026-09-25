using MediatR;
using Tooba.AccessControl.Domain;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Commands.ArchiveRole;

/// <summary>
/// فرمان آرشیو نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record ArchiveRoleCommand(
    Guid RoleId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    Guid ActorUserId,
    string? TenantId,
    string? TraceId) : IRequest<Unit>;

/// <summary>Handler فرمان آرشیو نقش.</summary>
public sealed class ArchiveRoleCommandHandler : IRequestHandler<ArchiveRoleCommand, Unit>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public ArchiveRoleCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Unit> Handle(ArchiveRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        await _directory.ArchiveRoleAsync(
            request.RoleId,
            owner,
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
        return Unit.Value;
    }
}
