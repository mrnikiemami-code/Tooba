using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Infrastructure.Directories;

/// <summary>
/// نگهبان باز موردکاربرد Payment. شمارهٔ پرداخت به‌تنهایی اجازهٔ جهش نیست.
/// </summary>
public sealed class OpenPaymentUseCaseGuard : IPaymentUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
