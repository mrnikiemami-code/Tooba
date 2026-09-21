using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain.Entities;

namespace Tooba.Settlement.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CommissionPolicySnapshot
{
    /// <summary>شناسه سیاست مرجع.</summary>
    public Guid PolicyId { get; init; }

    /// <summary>نام snapshot.</summary>
    public string PolicyName { get; init; } = string.Empty;

    /// <summary>نرخ snapshot.</summary>
    public decimal Rate { get; init; }

    /// <summary>snapshot را از سیاست فعال می‌سازد.</summary>
    public static CommissionPolicySnapshot FromPolicy(CommissionPolicy policy) =>
        new()
        {
            PolicyId = policy.PolicyId,
            PolicyName = policy.Name,
            Rate = policy.Rate,
        };
}
