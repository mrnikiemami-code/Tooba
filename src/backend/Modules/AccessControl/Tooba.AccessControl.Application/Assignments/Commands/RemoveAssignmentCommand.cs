using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Application.Permissions;

namespace Tooba.AccessControl.Application.Assignments.Commands;

/// <summary>
/// فرمان حذف تخصیص نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="AssignmentId">شناسهٔ تخصیص.</param>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record RemoveAssignmentCommand(
    Guid AssignmentId,
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    Guid ActorUserId,
    string? TenantId,
    string? TraceId) : IRequest<Result>;

/// <summary>Handler فرمان حذف تخصیص.</summary>
public sealed class RemoveAssignmentCommandHandler : IRequestHandler<RemoveAssignmentCommand, Result>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public RemoveAssignmentCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(RemoveAssignmentCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return AccessControlOperation.ExecuteAsync(() => _directory.RemoveAssignmentAsync(
            request.AssignmentId,
            owner,
            request.ActorUserId,
            request.TraceId,
            cancellationToken));
    }
}
