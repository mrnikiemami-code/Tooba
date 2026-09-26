using Microsoft.AspNetCore.Identity;
using Tooba.Identity.Application;

namespace Tooba.Identity.Infrastructure.PasswordHashing;

/// <summary>
/// پوشش <see cref="PasswordHasher{TUser}"/> بدون اختراع رمزنگاری.
/// </summary>
public sealed class AspNetPasswordHashingService : IPasswordHashingService
{
    private readonly PasswordHasher<object> _hasher = new();

    /// <inheritdoc />
    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    /// <inheritdoc />
    public PasswordVerificationOutcome Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(new object(), hash, password) switch
        {
            PasswordVerificationResult.Failed => PasswordVerificationOutcome.Failed,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Success,
        };
}
