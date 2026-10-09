using Tooba.Settlement.Application.Payouts.Ports;

namespace Tooba.Settlement.Infrastructure.Directories;

/// <summary>
/// نگهبان باز موردکاربرد Settlement. چرخهٔ mutate در این نسخه بدون محدودیت است و در صورت نیاز
/// (قفل/زمان‌بندی) از همین درز گسترش می‌یابد.
/// </summary>
public sealed class OpenSettlementUseCaseGuard : ISettlementUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
