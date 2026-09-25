using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Commands.AssignRole;

/// <summary>
/// فرمان تخصیص نقش به کاربر در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="UserId">شناسهٔ کاربر.</param>
/// <param name="RoleId">شناسهٔ نقش.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record AssignRoleCommand(
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    Guid UserId,
    Guid RoleId,
    Guid ActorUserId,
    string? TenantId,
    string? TraceId) : IRequest<UserRoleAssignmentDto>;

/// <summary>Handler فرمان تخصیص نقش.</summary>
public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, UserRoleAssignmentDto>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public AssignRoleCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<UserRoleAssignmentDto> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return _directory.AssignRoleAsync(
            owner,
            request.UserId,
            request.RoleId,
            request.ActorUserId,
            request.TraceId,
            cancellationToken);
    }
}
