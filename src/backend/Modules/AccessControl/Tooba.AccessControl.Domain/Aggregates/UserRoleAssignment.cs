using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Domain.Aggregates;

/// <summary>انتساب نقش به کاربر در محدودهٔ مالک.</summary>
public sealed class UserRoleAssignment
{
    /// <summary>شناسه.</summary>
    public Guid Id { get; set; }

    /// <summary>کاربر.</summary>
    public Guid UserId { get; set; }

    /// <summary>نقش.</summary>
    public Guid RoleId { get; set; }

    /// <summary>گونهٔ مالک.</summary>
    public AccessOwnerScopeKind OwnerScopeKind { get; set; }

    /// <summary>شناسهٔ مالک.</summary>
    public Guid? OwnerScopeId { get; set; }

    /// <summary>زمان تخصیص.</summary>
    public DateTimeOffset AssignedAt { get; set; }
}
