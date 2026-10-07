namespace Tooba.Payment.Application.Admin.Models;

/// <summary>Admin payments grid row (Payment-owned read model for the admin grid).</summary>
public sealed record AdminPaymentGridItemDto(
    Guid PaymentId,
    Guid CheckoutId,
    string OrderReference,
    string CustomerDisplayName,
    decimal Amount,
    string Currency,
    string Status,
    string ProviderCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string SupplyStatus = "NotApplicable",
    string ReservationLabel = "",
    string ReservationLabelEn = "",
    string ReservationState = "none",
    int? ReservationCycleNumber = null,
    bool ReservationRetryPossible = false,
    bool ReservationNeedsReacquire = false,
    bool ReservationRetryLimitReached = false);

public sealed record AdminPaymentGridPageDto(
    IReadOnlyList<AdminPaymentGridItemDto> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record AdminPaymentGridFilterInput(
    string Field,
    string Operator,
    string? Value,
    string? ValueTo,
    IReadOnlyList<string>? Values);

public sealed record AdminPaymentGridQueryInput(
    string? Search,
    IReadOnlyList<AdminPaymentGridFilterInput> Filters,
    string SortField,
    string SortDirection,
    int Page,
    int PageSize);
