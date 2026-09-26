using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Identity.Application;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Identity.Infrastructure.Sessions;

namespace Tooba.Identity.Infrastructure.Authentication;

/// <summary>
/// پیاده‌سازی موارد استفادهٔ احراز هویت Identity روی DbContext همین ماژول.
/// </summary>
public sealed class IdentityAuthenticationService : IIdentityAuthenticationService
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordHashingService _hasher;
    private readonly IOptions<IdentityPasswordPolicyOptions> _policy;
    private readonly IIdentitySecurityEventSink _security;
    private readonly IdentityLifecycleService _lifecycle;

    /// <summary>
    /// سرویس را با وابستگی‌های ماژول می‌سازد.
    /// </summary>
    public IdentityAuthenticationService(
        IdentityDbContext db,
        IPasswordHashingService hasher,
        IOptions<IdentityPasswordPolicyOptions> policy,
        IIdentitySecurityEventSink security,
        IdentityLifecycleService lifecycle)
    {
        _db = db;
        _hasher = hasher;
        _policy = policy;
        _security = security;
        _lifecycle = lifecycle;
    }

    /// <inheritdoc />
    public async Task<RegisterUserResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidatePassword(command.Password);
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(command.IdentifierKind, command.Identifier);
        var exists = await _db.Identifiers.AnyAsync(
            x => x.Kind == command.IdentifierKind && x.NormalizedValue == normalized,
            cancellationToken);
        if (exists)
        {
            throw new IdentityDuplicateIdentifierFault(normalized);
        }

        var now = DateTimeOffset.UtcNow;
        var user = UserAccount.Register(command.IdentifierKind, command.Identifier, now);
        user.Password = new PasswordCredential
        {
            UserId = user.UserId,
            PasswordHash = _hasher.Hash(command.Password),
            HasherFormatVersion = 1,
            UpdatedAt = now,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "credential_change", UserId = user.UserId, OccurredAt = now },
            cancellationToken);
        return new RegisterUserResult { UserId = user.UserId };
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> AuthenticateWithPasswordAsync(
        LoginIdentifierKind kind,
        string identifier,
        string password,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(password);
        var now = DateTimeOffset.UtcNow;
        string normalized;
        try
        {
            (_, normalized) = LoginIdentifierNormalizer.Normalize(kind, identifier);
        }
        catch (ArgumentException)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        var row = await _db.Identifiers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Kind == kind && x.NormalizedValue == normalized, cancellationToken);
        if (row is null)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        var user = await _db.Users.Include(x => x.Password).FirstAsync(x => x.UserId == row.UserId, cancellationToken);
        if (user.Status == UserAccountStatus.Disabled)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.Disabled);
        }

        if (user.Status == UserAccountStatus.Locked)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.Locked);
        }

        if (user.Password is null)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        var verify = _hasher.Verify(user.Password.PasswordHash, password);
        if (verify == PasswordVerificationOutcome.Failed)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        if (verify == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            user.Password.PasswordHash = _hasher.Hash(password);
            user.Password.UpdatedAt = now;
            user.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var ticket = await _lifecycle.EstablishSessionAsync(user, cancellationToken);
        await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_success", UserId = user.UserId, OccurredAt = now }, cancellationToken);
        return AuthenticationResult.Success(ticket);
    }

    /// <inheritdoc />
    public async Task<Guid?> FindUserIdByIdentifierAsync(LoginIdentifierKind kind, string identifier, CancellationToken cancellationToken)
    {
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(kind, identifier);
        var row = await _db.Identifiers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Kind == kind && x.NormalizedValue == normalized, cancellationToken);
        return row?.UserId;
    }

    /// <inheritdoc />
    public async Task DisableAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstAsync(x => x.UserId == userId, cancellationToken);
        user.Status = UserAccountStatus.Disabled;
        user.BumpSecurityStamp(DateTimeOffset.UtcNow);
        await _lifecycle.RevokeAllSessionsAsync(userId, "account_disable", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "account_disable", UserId = userId, OccurredAt = user.UpdatedAt },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task LockAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstAsync(x => x.UserId == userId, cancellationToken);
        user.Status = UserAccountStatus.Locked;
        user.BumpSecurityStamp(DateTimeOffset.UtcNow);
        await _lifecycle.RevokeAllSessionsAsync(userId, "account_lock", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "account_lock", UserId = userId, OccurredAt = user.UpdatedAt },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
        _lifecycle.ChangePasswordAsync(userId, currentPassword, newPassword, cancellationToken);

    /// <inheritdoc />
    public Task<AuthenticationResult> RefreshSessionAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken) =>
        _lifecycle.RefreshSessionAsync(sessionId, refreshToken, cancellationToken);

    /// <inheritdoc />
    public Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken) =>
        _lifecycle.RevokeSessionAsync(sessionId, reason, cancellationToken);

    /// <inheritdoc />
    public Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken) =>
        _lifecycle.RevokeAllSessionsAsync(userId, reason, cancellationToken);

    /// <inheritdoc />
    public async Task<AuthenticationResult> EstablishSessionForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user is null)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        if (user.Status == UserAccountStatus.Disabled)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.Disabled);
        }

        if (user.Status == UserAccountStatus.Locked)
        {
            await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now }, cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.Locked);
        }

        var ticket = await _lifecycle.EstablishSessionAsync(user, cancellationToken);
        await _security.RecordAsync(new IdentitySecurityEvent { EventName = "login_success", UserId = user.UserId, OccurredAt = now }, cancellationToken);
        return AuthenticationResult.Success(ticket);
    }

    private void ValidatePassword(string password)
    {
        var policy = _policy.Value;
        if (password.Length < policy.MinimumLength)
        {
            throw new ArgumentException(IdentityErrorCodes.ValidationFailed, nameof(password));
        }

        if (policy.RequireLetterAndDigit && !(password.Any(char.IsLetter) && password.Any(char.IsDigit)))
        {
            throw new ArgumentException(IdentityErrorCodes.ValidationFailed, nameof(password));
        }
    }
}
