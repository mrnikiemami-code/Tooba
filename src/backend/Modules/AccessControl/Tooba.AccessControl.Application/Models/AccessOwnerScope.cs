using Tooba.AccessControl.Contracts.Enums;

namespace Tooba.AccessControl.Application.Models;

/// <summary>زمینهٔ مالک برای عملیات دایرکتوری.</summary>
/// <param name="Kind">گونهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک.</param>
/// <param name="TenantId">Tenant اختیاری.</param>
public sealed record AccessOwnerScope(AccessOwnerScopeKind Kind, Guid? OwnerScopeId, string? TenantId = null);
