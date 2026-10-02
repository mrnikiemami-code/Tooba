using Tooba.Identity.Application.Models;

namespace Tooba.Identity.Application.Ports;

/// <summary>
/// هش استاندارد رمز؛ الگوریتم سفارشی نیست.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// هش را می‌سازد. ورودی plaintext لاگ نمی‌شود.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// صحت را می‌سنجد و در صورت نیاز نشانهٔ rehash می‌دهد.
    /// </summary>
    PasswordVerificationOutcome Verify(string hash, string password);
}
