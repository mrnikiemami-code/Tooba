using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// نگهبان باز موردکاربرد Returns (پیاده‌سازی پیش‌فرض: بدون محدودیت).
/// </summary>
public sealed class OpenReturnUseCaseGuard : IReturnUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
