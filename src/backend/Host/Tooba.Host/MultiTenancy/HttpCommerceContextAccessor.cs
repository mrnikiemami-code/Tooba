using Tooba.BuildingBlocks;

namespace Tooba.Host.MultiTenancy;

/// <summary>
/// نگهداشت <see cref="CommerceContext"/> روی HttpContext.Items. هدر Tenant منبع حقیقت نیست.
/// </summary>
internal sealed class HttpCommerceContextAccessor : ICurrentCommerceContext, ICurrentEdition, ICurrentTenant, ICommerceContextAssigner
{
    /// <summary>
    /// کلید Items برای زمینهٔ تثبیت‌شدهٔ همین درخواست.
    /// </summary>
    internal const string ItemKey = "Tooba.CommerceContext";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private CommerceContext? _assigned;

    /// <summary>
    /// accessor را به HttpContext درخواست وصل می‌کند. کارگر می‌تواند بدون Host مقدار بگذارد.
    /// </summary>
    public HttpCommerceContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public CommerceContext? Current =>
        _assigned ?? _httpContextAccessor.HttpContext?.Items[ItemKey] as CommerceContext;

    /// <inheritdoc />
    public void Assign(CommerceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _assigned = context;
    }

    /// <inheritdoc />
    EditionContext? ICurrentEdition.Current => Current?.Edition;

    /// <inheritdoc />
    TenantContext? ICurrentTenant.Current => Current?.Tenant;
}
