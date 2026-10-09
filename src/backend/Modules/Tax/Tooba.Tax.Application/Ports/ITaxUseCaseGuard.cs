namespace Tooba.Tax.Application.Ports;

/// <summary>
/// نگهبان موردکاربرد Tax.
/// </summary>
public interface ITaxUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن قاعده و طبقه را بررسی می‌کند.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}
