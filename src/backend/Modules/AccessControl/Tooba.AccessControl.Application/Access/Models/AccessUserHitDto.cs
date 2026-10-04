namespace Tooba.AccessControl.Application.Access.Models;

/// <summary>کاربر قابل جستجو در محدوده.</summary>
public sealed record AccessUserHitDto(
    Guid UserId,
    IReadOnlyList<string> RoleCodes,
    string? DisplayName = null,
    string? Email = null,
    string? Mobile = null);
