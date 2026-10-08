using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Ports;


/// <summary>
/// نگهبان use-case مرجوعی.
/// </summary>
public interface IReturnUseCaseGuard
{
    /// <summary>اجازهٔ mutate را بررسی می‌کند.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}
