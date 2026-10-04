using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Roles.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Application.Permissions;

namespace Tooba.AccessControl.Application.Roles.Commands;

/// <summary>
/// فرمان ایجاد نقش در محدودهٔ مالک مشخص.
/// </summary>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="ActorUserId">شناسهٔ Actor مجاز (از لایهٔ مجوز).</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="Name">نام نقش.</param>
/// <param name="Code">کد نقش.</param>
/// <param name="Description">توضیح.</param>
/// <param name="TraceId">شناسهٔ رهگیری درخواست.</param>
public sealed record CreateRoleCommand(
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    Guid ActorUserId,
    string? TenantId,
    string Name,
    string Code,
    string Description,
    string? TraceId) : IRequest<Result<AccessRoleDto>>;

/// <summary>Handler فرمان ایجاد نقش.</summary>
public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<AccessRoleDto>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public CreateRoleCommandHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<AccessRoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        return AccessControlOperation.ExecuteAsync(() => _directory.CreateRoleAsync(
            owner,
            new CreateRoleRequest(request.Name, request.Code, request.Description),
            request.ActorUserId,
            request.TraceId,
            cancellationToken));
    }
}
