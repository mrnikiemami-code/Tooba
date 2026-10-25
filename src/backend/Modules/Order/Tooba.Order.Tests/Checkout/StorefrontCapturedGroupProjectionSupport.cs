using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Tests.Checkout;

/// <summary>ساعت قابل تنظیم برای اثبات تبدیل تاریخ محلی و خواندن واحد.</summary>
internal sealed class MutableClock(DateTimeOffset utcNow) : IClock
{
    internal DateTimeOffset Value { get; set; } = utcNow;

    internal int Reads { get; private set; }

    public DateTimeOffset UtcNow
    {
        get
        {
            Reads++;
            return Value;
        }
    }
}

/// <summary>
/// نامزد کنترل‌شدهٔ Contracts مالک Shipping: پاسخ از پیش آماده، بدون ادعای provider واقعی.
/// هر فراخوانی ثبت می‌شود تا تعداد/توکن/ورودی‌ها قابل اثبات باشند.
/// </summary>
internal sealed class RecordingCapturedQuoteLookup : IShippingCapturedRevisionQuoteLookup
{
    private readonly List<ShippingCapturedQuoteLookupRequest> _requests = [];

    /// <summary>پاسخ آماده برای ورودی؛ تهی یعنی نتیجهٔ تجاری ناموفق.</summary>
    internal Func<ShippingCapturedQuoteLookupRequest, ShippingCapturedQuoteResult?>? Respond { get; set; }

    /// <summary>خطای واقعی درز بیرونی (برای اثبات انتشار).</summary>
    internal Exception? Failure { get; set; }

    internal IReadOnlyList<ShippingCapturedQuoteLookupRequest> Requests => _requests;

    internal int Calls => _requests.Count;

    internal CancellationToken? LastToken { get; private set; }

    public Task<Result<ShippingCapturedQuoteResult>> QuoteAsync(
        ShippingCapturedQuoteLookupRequest request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);
        LastToken = cancellationToken;

        if (Failure is not null)
        {
            return Task.FromException<Result<ShippingCapturedQuoteResult>>(Failure);
        }

        var response = Respond?.Invoke(request);
        return Task.FromResult(response is null
            ? Result.Failure<ShippingCapturedQuoteResult>(
                new SemanticError(StorefrontCapturedGroupProjectionReasons.QuoteFailed))
            : Result.Success(response));
    }
}

/// <summary>درز فکت مالی آماده (برای اثبات رد شدن فکت ساختگی/ناسازگار).</summary>
internal sealed class FixedGoodsFactsReader(CheckoutShippingGroupGoodsFactsResult? result) : ICheckoutShippingGroupGoodsFactsReader
{
    internal int Calls { get; private set; }

    public Task<CheckoutShippingGroupGoodsFactsResult> ReadAsync(
        CheckoutShippingGroupGoodsFactsRequest request,
        CancellationToken cancellationToken)
    {
        Calls++;
        return result is null
            ? throw new InvalidOperationException("facts must not be fabricated")
            : Task.FromResult(result);
    }
}

/// <summary>پیکربندی تهی برای ثبت ماژول در تست DI بدون هیچ مقدار تنظیمات.</summary>
internal sealed class EmptyTestConfiguration : Microsoft.Extensions.Configuration.IConfiguration
{
    public string? this[string key]
    {
        get => null;
        set { }
    }

    public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];

    public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() =>
        throw new NotSupportedException();

    public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) =>
        new Section(key);

    private sealed class Section(string key) : Microsoft.Extensions.Configuration.IConfigurationSection
    {
        public string? this[string childKey]
        {
            get => null;
            set { }
        }

        public string Key => key;

        public string Path => key;

        public string? Value { get; set; }

        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => [];

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() =>
            throw new NotSupportedException();

        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string childKey) =>
            new Section(childKey);
    }
}