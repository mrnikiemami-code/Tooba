namespace Tooba.Payment.Application.Ports;

/// <summary>DTO override مهلت روش پرداخت برای Host بدون PaymentDbContext.</summary>
public sealed record PaymentMethodHoldOverrideDto(
    string ProviderCode,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours);

/// <summary>درگاه تنظیمات مهلت روش پرداخت متعلق به Payment.</summary>
public interface IPaymentHoldSettingsDirectory
{
    /// <summary>فهرست overrideهای روش پرداخت.</summary>
    Task<IReadOnlyList<PaymentMethodHoldOverrideDto>> ListMethodOverridesAsync(CancellationToken cancellationToken);

    /// <summary>جایگزینی/به‌روزرسانی یک روش.</summary>
    Task UpsertMethodOverrideAsync(
        string providerCode,
        int? onlinePaymentHoldHours,
        int? manualPaymentInitialHoldHours,
        int? manualPaymentReviewHoldHours,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>سطر پرداخت برای ترکیب Host (بدون نشت DbContext).</summary>
public sealed record PaymentCheckoutRowDto(
    Guid PaymentId,
    Guid CheckoutId,
    decimal Amount,
    string Currency,
    string Status,
    string ProviderCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? EvidenceSubmittedAt);

/// <summary>پرس‌وجوی خواندنی پرداخت برای Storefront/Admin بدون PaymentDbContext در Host.</summary>
public interface IPaymentQueryDirectory
{
    /// <summary>آخرین پرداخت هر checkout به‌همراه EvidenceSubmittedAt تلاش آخر.</summary>
    Task<IReadOnlyList<PaymentCheckoutRowDto>> GetLatestByCheckoutIdsAsync(
        IReadOnlyCollection<Guid> checkoutIds,
        CancellationToken cancellationToken);

    /// <summary>صفحهٔ خام پرداخت برای Admin grid (فیلتر/مرتب‌سازی در ماژول).</summary>
    Task<PaymentAdminGridPageDto> QueryAdminGridAsync(
        PaymentAdminGridQueryDto query,
        CancellationToken cancellationToken);
}

/// <summary>ورودی grid ادمین پرداخت.</summary>
public sealed record PaymentAdminGridQueryDto(
    string? Search,
    IReadOnlyList<PaymentAdminGridFilterDto> Filters,
    string SortField,
    string SortDirection,
    int Page,
    int PageSize,
    IReadOnlyList<Guid>? RestrictCheckoutIds);

/// <summary>فیلتر سادهٔ grid.</summary>
public sealed record PaymentAdminGridFilterDto(
    string Field,
    string Operator,
    string? Value,
    string? ValueTo,
    IReadOnlyList<string>? Values);

/// <summary>صفحهٔ نتیجهٔ grid.</summary>
public sealed record PaymentAdminGridPageDto(
    IReadOnlyList<PaymentCheckoutRowDto> Items,
    int Total);
