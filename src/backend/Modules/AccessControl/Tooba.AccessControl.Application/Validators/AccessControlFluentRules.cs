using System.Linq.Expressions;
using FluentValidation;

namespace Tooba.AccessControl.Application.Validators;

/// <summary>
/// Reusable FluentValidation fragments for AccessControl transport input.
/// Only primitive request shape is checked here. Role existence, role mutability, duplicate role code,
/// seller ceiling, permission delegation, permission catalog membership, category existence, escalation,
/// assignment uniqueness and domain ownership/state remain owned by Application/Domain.
/// </summary>
public static class AccessControlFluentRules
{
    /// <summary>Maximum persisted role name length (`access_roles.name` = 128).</summary>
    public const int RoleNameMaxLength = 128;

    /// <summary>Maximum persisted role code length (`access_roles.code` = 64).</summary>
    public const int RoleCodeMaxLength = 64;

    /// <summary>Minimum established role code length (mirrors the existing domain lexical contract).</summary>
    public const int RoleCodeMinLength = 2;

    /// <summary>Maximum persisted role description length (`access_roles.description` = 512).</summary>
    public const int RoleDescriptionMaxLength = 512;

    /// <summary>Maximum persisted permission identifier length (`permission_id` = 128).</summary>
    public const int PermissionIdMaxLength = 128;

    /// <summary>Requires a non-empty identifier.</summary>
    public static void RequireId<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> selector,
        string errorCode)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(errorCode);

    /// <summary>Requires the envelope to be supplied (non-null).</summary>
    public static void RequireEnvelope<T, TItem>(
        AbstractValidator<T> validator,
        Expression<Func<T, IReadOnlyList<TItem>>> selector,
        string errorCode)
        => validator.RuleFor(selector).NotNull().WithErrorCode(errorCode);

    /// <summary>Requires non-blank text after trimming.</summary>
    public static void RequireNonBlank<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> selector,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(errorCode);

    /// <summary>Requires non-blank text whose trimmed length is within <paramref name="maxLength"/>.</summary>
    public static void RequireNonBlankWithin<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> selector,
        int maxLength,
        string requiredErrorCode,
        string lengthErrorCode)
    {
        validator.RuleFor(selector)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(requiredErrorCode);
        validator.RuleFor(selector)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= maxLength)
            .WithErrorCode(lengthErrorCode);
    }

    /// <summary>Optional text: absent is allowed; a supplied value must stay within <paramref name="maxLength"/>.</summary>
    public static void OptionalWithin<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        int maxLength,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Trim().Length <= maxLength)
            .WithErrorCode(errorCode);

    /// <summary>Requires the established primitive role-code lexical/length shape.</summary>
    public static void RequireRoleCodeShape<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string>> selector,
        string requiredErrorCode,
        string shapeErrorCode)
    {
        validator.RuleFor(selector)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(requiredErrorCode);
        validator.RuleFor(selector)
            .Must(value =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }

                var trimmed = value.Trim();
                return trimmed.Length is >= RoleCodeMinLength and <= RoleCodeMaxLength
                    && trimmed.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
            })
            .WithErrorCode(shapeErrorCode);
    }

    /// <summary>True when a permission identifier is transport-shaped (non-blank and within length).</summary>
    public static bool IsPermissionIdNonBlank(string? permissionId)
        => !string.IsNullOrWhiteSpace(permissionId);

    /// <summary>True when a permission identifier is absent or within the persisted transport length.</summary>
    public static bool IsPermissionIdWithin(string? permissionId, int maxLength)
        => string.IsNullOrWhiteSpace(permissionId) || permissionId.Trim().Length <= maxLength;
}
