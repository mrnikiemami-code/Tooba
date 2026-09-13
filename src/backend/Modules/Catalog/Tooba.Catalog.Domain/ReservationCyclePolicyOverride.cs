namespace Tooba.Catalog.Domain;

/// <summary>override چرخه رزرو در سطح Offer یا Category.</summary>
public sealed class ReservationCyclePolicyOverride
{
    /// <summary>سطح Offer.</summary>
    public const string OfferScope = "offer";

    /// <summary>سطح Category.</summary>
    public const string CategoryScope = "category";

    /// <summary>شناسه ردیف.</summary>
    public Guid OverrideId { get; init; }

    /// <summary>offer یا category.</summary>
    public string ScopeKind { get; init; } = OfferScope;

    /// <summary>شناسه Offer یا Category.</summary>
    public Guid ScopeId { get; init; }

    /// <summary>مهلت اولیه دقیقه‌ای.</summary>
    public int? InitialReservationHoldMinutes { get; private set; }

    /// <summary>مهلت retry دقیقه‌ای.</summary>
    public int? RetryReservationHoldMinutes { get; private set; }

    /// <summary>سقف چرخه.</summary>
    public int? MaxReservationCycles { get; private set; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف جدید.</summary>
    public static ReservationCyclePolicyOverride Create(
        string scopeKind,
        Guid scopeId,
        int? initialMinutes,
        int? retryMinutes,
        int? maxCycles,
        DateTimeOffset now)
    {
        var row = new ReservationCyclePolicyOverride
        {
            OverrideId = Guid.NewGuid(),
            ScopeKind = scopeKind,
            ScopeId = scopeId,
            UpdatedAt = now,
        };
        return row.Apply(initialMinutes, retryMinutes, maxCycles, now);
    }

    /// <summary>مقادیر را با بازه معتبر می‌نویسد.</summary>
    public ReservationCyclePolicyOverride Apply(
        int? initialMinutes,
        int? retryMinutes,
        int? maxCycles,
        DateTimeOffset now)
    {
        InitialReservationHoldMinutes = Clamp(initialMinutes, 1, 24 * 60 * 30);
        RetryReservationHoldMinutes = Clamp(retryMinutes, 1, 24 * 60 * 30);
        MaxReservationCycles = Clamp(maxCycles, 1, 20);
        UpdatedAt = now;
        return this;
    }

    private static int? Clamp(int? value, int min, int max)
    {
        if (value is null)
        {
            return null;
        }

        return Math.Clamp(value.Value, min, max);
    }
}
