using Tooba.Promotion.Application.Ports;

namespace Tooba.Promotion.Infrastructure.Directories;

/// <summary>
/// نگهبان باز نوشتن پروموشن. ماتریس SpiceDB اینجا قفل نمی‌شود.
/// </summary>
public sealed class OpenPromotionUseCaseGuard : IPromotionUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
