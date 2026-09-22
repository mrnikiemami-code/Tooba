using Tooba.Cart.Application.Ports;
using Tooba.Cart.Contracts;

namespace Tooba.Cart.Application.Conversion;

/// <summary>آداپتر در-فرآیند تبدیل سبد؛ بعداً می‌تواند HTTP/gRPC/message شود.</summary>
public sealed class CartConversionAdapter : ICartConversionPort
{
    private readonly ICartDirectory _carts;

    /// <summary>آداپتر را می‌سازد.</summary>
    public CartConversionAdapter(ICartDirectory carts) => _carts = carts;

    /// <inheritdoc />
    public async Task<CartConversionResult> ConvertForCheckoutAsync(
        CartConversionRequest request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _carts.ConvertAsync(
            request.CartId,
            request.Access,
            request.ExpectedVersion,
            request.Intent,
            cancellationToken);
        return new CartConversionResult(
            snapshot.CartId,
            snapshot.Version,
            snapshot.Status,
            snapshot.ConversionIntent);
    }
}
