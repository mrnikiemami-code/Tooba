using Tooba.BuildingBlocks;

namespace Tooba.Settlement.Domain.Entities;

/// <summary>
/// Domain type.
/// </summary>
public sealed class CommissionPolicy
{
    private CommissionPolicy()
    {
    }

    /// <summary>شناسه سیاست.</summary>
    public Guid PolicyId { get; init; }

    /// <summary>نام نمایشی.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>نرخ کارمزد (۰.۱۰ = ۱۰٪).</summary>
    public decimal Rate { get; init; }

    /// <summary>آیا پیش‌فرض marketplace است.</summary>
    public bool IsDefault { get; init; }

    /// <summary>زمان اعتبار.</summary>
    public DateTimeOffset EffectiveFrom { get; init; }

    /// <summary>سیاست پیش‌فرض ۱۰٪ marketplace را می‌سازد.</summary>
    public static CommissionPolicy CreateDefaultMarketplace(DateTimeOffset now) =>
        new()
        {
            PolicyId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            Name = "marketplace-default-10pct",
            Rate = 0.10m,
            IsDefault = true,
            EffectiveFrom = now,
        };
}
