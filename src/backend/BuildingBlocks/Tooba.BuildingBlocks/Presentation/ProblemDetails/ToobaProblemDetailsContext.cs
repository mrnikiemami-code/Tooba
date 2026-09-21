namespace Tooba.BuildingBlocks.Presentation.ProblemDetails;

/// <summary>
/// زمینهٔ تغییرناپذیر برای لاگ ساختاریافته و ProblemDetails — بدون DB و بدون secret.
/// </summary>
/// <param name="CorrelationId">شناسه همبستگی نرمال‌شده.</param>
/// <param name="TraceId">TraceId تله‌متری یا TraceIdentifier.</param>
/// <param name="SpanId">SpanId فعال یا تهی.</param>
/// <param name="RequestId">TraceIdentifier درخواست.</param>
/// <param name="TenantId">Tenant در صورت موجود بودن امن.</param>
/// <param name="StoreId">Store در صورت موجود بودن امن.</param>
/// <param name="ActorId">شناسه کاربر/بازیگر در صورت موجود بودن امن.</param>
/// <param name="Path">مسیر HTTP.</param>
/// <param name="Method">متد HTTP.</param>
/// <param name="HideExceptionDetails">در Production باید true باشد.</param>
public sealed record ToobaProblemDetailsContext(
    string CorrelationId,
    string TraceId,
    string? SpanId,
    string? RequestId,
    string? TenantId,
    string? StoreId,
    string? ActorId,
    string? Path,
    string? Method,
    bool HideExceptionDetails);
