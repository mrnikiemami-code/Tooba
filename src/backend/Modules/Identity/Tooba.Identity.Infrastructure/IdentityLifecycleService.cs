using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// هش SHA-256 استاندارد BCL برای رازهای Refresh/OTP. الگوریتم اختصاصی اختراع نمی‌شود و plaintext persist نمی‌شود.
/// </summary>
public static class OpaqueSecretHasher
{
    /// <summary>
    /// راز با آنتروپی بالا می‌سازد. فقط در مرز صدور برگردانده می‌شود.
    /// </summary>
    public static string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// کد عددی برای OTP پیامکی/ایمیلی از RNG نه از Random ضعیف.
    /// </summary>
    public static string GenerateNumericCode(int digits)
    {
        if (digits is < 4 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(digits));
        }

        var max = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{digits}");
    }

    /// <summary>
    /// هش پایدار برای ذخیره. ورودی خام لاگ نمی‌شود.
    /// </summary>
    public static string Hash(string rawSecret)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawSecret);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret)));
    }
}

/// <summary>
/// بلیت دسترسی همان نشست است؛ JWT سفارشی ساخته نمی‌شود تا cookie/BFF/IdP بعداً جایگزین شوند.
/// </summary>
public sealed class SessionAccessCredentialBoundary : IAccessCredentialBoundary
{
    /// <inheritdoc />
    public AuthenticationTicket ToAccessTicket(AuthenticationTicket sessionTicket)
    {
        ArgumentNullException.ThrowIfNull(sessionTicket);
        return sessionTicket with { RefreshToken = null };
    }
}

/// <summary>
/// نشست، چرخش Refresh، چالش پایدار و تغییر اعتبار روی schema Identity. Host parse نمی‌شود.
/// </summary>
public sealed class IdentityLifecycleService : IIdentityCredentialLifecycle, IOtpChallengeService
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordHashingService _hasher;
    private readonly IOptions<IdentityPasswordPolicyOptions> _passwordPolicy;
    private readonly IOptions<IdentityLifecycleOptions> _lifecycle;
    private readonly IIdentitySecurityEventSink _security;
    private readonly ICurrentCommerceContext _commerce;
    private readonly IOtpSender _sender;

    /// <summary>
    /// سرویس چرخهٔ عمر را به DbContext Tenant-aware و فرستندهٔ انتزاعی وصل می‌کند.
    /// </summary>
    public IdentityLifecycleService(
        IdentityDbContext db,
        IPasswordHashingService hasher,
        IOptions<IdentityPasswordPolicyOptions> passwordPolicy,
        IOptions<IdentityLifecycleOptions> lifecycle,
        IIdentitySecurityEventSink security,
        ICurrentCommerceContext commerce,
        IOtpSender sender)
    {
        _db = db;
        _hasher = hasher;
        _passwordPolicy = passwordPolicy;
        _lifecycle = lifecycle;
        _security = security;
        _commerce = commerce;
        _sender = sender;
    }

    /// <summary>
    /// نشست جدید با راز Refresh هش‌شده می‌سازد. راز خام فقط در بلیت برمی‌گردد.
    /// </summary>
    public async Task<AuthenticationTicket> EstablishSessionAsync(UserAccount user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var now = DateTimeOffset.UtcNow;
        var raw = OpaqueSecretHasher.Generate();
        var ctx = _commerce.Current;
        var session = new AuthSession
        {
            SessionId = UuidV7.New(),
            UserId = user.UserId,
            CreatedAt = now,
            ExpiresAt = now.AddHours(_lifecycle.Value.SessionLifetimeHours),
            LastUsedAt = now,
            CredentialVersion = user.SecurityStamp,
            Edition = ctx?.Edition.Edition ?? ToobaEdition.Unset,
            TenantId = ctx?.Tenant?.TenantId.Value,
            RefreshSecretHash = OpaqueSecretHasher.Hash(raw),
            RefreshFamilyId = UuidV7.New(),
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "session_created", UserId = user.UserId, OccurredAt = now },
            cancellationToken);
        return new AuthenticationTicket
        {
            UserId = user.UserId,
            SessionHandle = session.SessionId,
            RefreshToken = raw,
            AuthenticatedAt = now,
        };
    }

    /// <summary>
    /// Refresh را می‌چرخاند. راز قبلی پس از موفقیت نامعتبر است؛ reuse راز قبلی خانواده را لغو می‌کند.
    /// </summary>
    public async Task<AuthenticationResult> RefreshSessionAsync(Guid sessionId, string refreshToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);
        var now = DateTimeOffset.UtcNow;
        var presented = OpaqueSecretHasher.Hash(refreshToken);
        var session = await _db.Sessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
        if (session is null)
        {
            return AuthenticationResult.Fail(AuthenticationOutcome.RevokedSession);
        }

        var user = await _db.Users.FirstAsync(x => x.UserId == session.UserId, cancellationToken);
        if (user.Status is UserAccountStatus.Disabled or UserAccountStatus.Locked)
        {
            var outcome = user.Status == UserAccountStatus.Disabled
                ? AuthenticationOutcome.Disabled
                : AuthenticationOutcome.Locked;
            await _security.RecordAsync(
                new IdentitySecurityEvent { EventName = "login_failure", UserId = user.UserId, OccurredAt = now },
                cancellationToken);
            return AuthenticationResult.Fail(outcome);
        }

        if (session.PreviousRefreshSecretHash is not null
            && string.Equals(session.PreviousRefreshSecretHash, presented, StringComparison.Ordinal))
        {
            await RevokeAllSessionsAsync(user.UserId, "refresh_reuse", cancellationToken);
            await _security.RecordAsync(
                new IdentitySecurityEvent { EventName = "refresh_reuse_detected", UserId = user.UserId, OccurredAt = now },
                cancellationToken);
            return AuthenticationResult.Fail(AuthenticationOutcome.RefreshReuse);
        }

        if (!string.Equals(session.RefreshSecretHash, presented, StringComparison.Ordinal)
            || !session.IsRefreshable(now, user.SecurityStamp))
        {
            return AuthenticationResult.Fail(AuthenticationOutcome.RevokedSession);
        }

        var nextRaw = OpaqueSecretHasher.Generate();
        session.PreviousRefreshSecretHash = session.RefreshSecretHash;
        session.RefreshSecretHash = OpaqueSecretHasher.Hash(nextRaw);
        session.LastUsedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return AuthenticationResult.Success(new AuthenticationTicket
        {
            UserId = user.UserId,
            SessionHandle = session.SessionId,
            RefreshToken = nextRaw,
            AuthenticatedAt = now,
        });
    }

    /// <summary>
    /// یک نشست را لغو می‌کند تا Refresh بعدی شکست بخورد.
    /// </summary>
    public async Task RevokeSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.RevokedAt = now;
        session.RevocationReason = reason;
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "session_revoked", UserId = session.UserId, OccurredAt = now },
            cancellationToken);
    }

    /// <summary>
    /// همهٔ نشست‌های زندهٔ User را لغو می‌کند.
    /// </summary>
    public async Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = await _db.Sessions
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = now;
            session.RevocationReason = reason;
        }

        if (sessions.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            await _security.RecordAsync(
                new IdentitySecurityEvent { EventName = "session_revoked", UserId = userId, OccurredAt = now },
                cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<OtpChallengeHandle> IssueAsync(OtpPurpose purpose, string destination, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        if (!Enum.IsDefined(purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }

        var raw = purpose == OtpPurpose.PasswordReset
            ? OpaqueSecretHasher.Generate()
            : OpaqueSecretHasher.GenerateNumericCode(8);
        var handle = await PersistChallengeAsync(purpose, userId: null, destination, raw, cancellationToken);
        await _sender.SendAsync(purpose, destination, raw, cancellationToken);
        return handle;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(OtpChallengeHandle handle, string oneTimeCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handle);
        var outcome = await ConsumeChallengeAsync(handle.ChallengeId, handle.Purpose, oneTimeCode, cancellationToken);
        return outcome == ChallengeConsumeOutcome.Succeeded;
    }

    /// <inheritdoc />
    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(
        LoginIdentifierKind kind,
        string identifier,
        CancellationToken cancellationToken)
    {
        Guid? challengeId = null;
        try
        {
            var (_, normalized) = LoginIdentifierNormalizer.Normalize(kind, identifier);
            var row = await _db.Identifiers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Kind == kind && x.NormalizedValue == normalized, cancellationToken);
            if (row is not null)
            {
                var raw = OpaqueSecretHasher.Generate();
                var handle = await PersistChallengeAsync(OtpPurpose.PasswordReset, row.UserId, normalized, raw, cancellationToken);
                challengeId = handle.ChallengeId;
                await _sender.SendAsync(OtpPurpose.PasswordReset, identifier, raw, cancellationToken);
            }
        }
        catch (ArgumentException)
        {
            // پاسخ عمومی یکسان می‌ماند تا قالب شناسه هم حساب را لو ندهد.
        }

        return new PasswordResetRequestResult { Accepted = true, ChallengeId = challengeId };
    }

    /// <inheritdoc />
    public async Task<ChallengeConsumeOutcome> CompletePasswordResetAsync(
        Guid challengeId,
        string secret,
        string newPassword,
        CancellationToken cancellationToken)
    {
        ValidatePassword(newPassword);
        var outcome = await ConsumeChallengeAsync(challengeId, OtpPurpose.PasswordReset, secret, cancellationToken);
        if (outcome != ChallengeConsumeOutcome.Succeeded)
        {
            return outcome;
        }

        var challenge = await _db.Challenges.FirstAsync(x => x.ChallengeId == challengeId, cancellationToken);
        if (challenge.UserId is null)
        {
            return ChallengeConsumeOutcome.InvalidOrExpired;
        }

        var now = DateTimeOffset.UtcNow;
        var user = await _db.Users.Include(x => x.Password).FirstAsync(x => x.UserId == challenge.UserId.Value, cancellationToken);
        user.Password ??= new PasswordCredential { UserId = user.UserId, HasherFormatVersion = 1 };
        user.Password.PasswordHash = _hasher.Hash(newPassword);
        user.Password.UpdatedAt = now;
        user.BumpSecurityStamp(now);
        await RevokeAllSessionsAsync(user.UserId, "password_reset", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "password_reset_completed", UserId = user.UserId, OccurredAt = now },
            cancellationToken);
        return ChallengeConsumeOutcome.Succeeded;
    }

    /// <inheritdoc />
    public async Task<OtpChallengeHandle> IssueIdentifierVerificationAsync(
        Guid userId,
        LoginIdentifierKind kind,
        string identifier,
        CancellationToken cancellationToken)
    {
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(kind, identifier);
        var owned = await _db.Identifiers.AnyAsync(
            x => x.UserId == userId && x.Kind == kind && x.NormalizedValue == normalized,
            cancellationToken);
        if (!owned)
        {
            throw new InvalidOperationException("شناسه در این دامنهٔ هویت به این User تعلق ندارد.");
        }

        var raw = OpaqueSecretHasher.GenerateNumericCode(8);
        var handle = await PersistChallengeAsync(OtpPurpose.IdentifierVerification, userId, normalized, raw, cancellationToken);
        await _sender.SendAsync(OtpPurpose.IdentifierVerification, identifier, raw, cancellationToken);
        return handle;
    }

    /// <inheritdoc />
    public async Task<ChallengeConsumeOutcome> CompleteIdentifierVerificationAsync(
        Guid challengeId,
        string secret,
        CancellationToken cancellationToken)
    {
        var outcome = await ConsumeChallengeAsync(challengeId, OtpPurpose.IdentifierVerification, secret, cancellationToken);
        if (outcome != ChallengeConsumeOutcome.Succeeded)
        {
            return outcome;
        }

        var challenge = await _db.Challenges.FirstAsync(x => x.ChallengeId == challengeId, cancellationToken);
        if (challenge.UserId is null)
        {
            return ChallengeConsumeOutcome.InvalidOrExpired;
        }

        var identifiers = await _db.Identifiers
            .Where(x => x.UserId == challenge.UserId)
            .ToListAsync(cancellationToken);
        var identifier = identifiers.FirstOrDefault(x => OpaqueSecretHasher.Hash(x.NormalizedValue) == challenge.IdentifierHash);
        if (identifier is null)
        {
            return ChallengeConsumeOutcome.InvalidOrExpired;
        }

        var now = DateTimeOffset.UtcNow;
        identifier.VerificationState = IdentifierVerificationState.Verified;
        identifier.VerifiedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "identifier_verified", UserId = challenge.UserId, OccurredAt = now },
            cancellationToken);
        return ChallengeConsumeOutcome.Succeeded;
    }

    /// <summary>
    /// تغییر رمز احرازشده: رمز جاری را می‌سنجد، هش را عوض می‌کند، مهر را جلو می‌برد و نشست‌ها را لغو می‌کند.
    /// </summary>
    public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        ValidatePassword(newPassword);
        var user = await _db.Users.Include(x => x.Password).FirstAsync(x => x.UserId == userId, cancellationToken);
        if (user.Password is null || _hasher.Verify(user.Password.PasswordHash, currentPassword) == PasswordVerificationOutcome.Failed)
        {
            throw new InvalidOperationException("رمز جاری نادرست است.");
        }

        var now = DateTimeOffset.UtcNow;
        user.Password.PasswordHash = _hasher.Hash(newPassword);
        user.Password.UpdatedAt = now;
        user.BumpSecurityStamp(now);
        await RevokeAllSessionsAsync(userId, "password_change", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await _security.RecordAsync(
            new IdentitySecurityEvent { EventName = "password_changed", UserId = userId, OccurredAt = now },
            cancellationToken);
    }

    private async Task<OtpChallengeHandle> PersistChallengeAsync(
        OtpPurpose purpose,
        Guid? userId,
        string identifierMaterial,
        string rawSecret,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = new AuthChallenge
        {
            ChallengeId = UuidV7.New(),
            UserId = userId,
            IdentifierHash = OpaqueSecretHasher.Hash(identifierMaterial),
            Purpose = purpose,
            SecretHash = OpaqueSecretHasher.Hash(rawSecret),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_lifecycle.Value.ChallengeLifetimeMinutes),
        };
        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);
        return new OtpChallengeHandle { ChallengeId = challenge.ChallengeId, Purpose = purpose };
    }

    private async Task<ChallengeConsumeOutcome> ConsumeChallengeAsync(
        Guid challengeId,
        OtpPurpose expectedPurpose,
        string secret,
        CancellationToken cancellationToken)
    {
        var challenge = await _db.Challenges.FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken);
        if (challenge is null)
        {
            return ChallengeConsumeOutcome.InvalidOrExpired;
        }

        var now = DateTimeOffset.UtcNow;
        if (challenge.LockedAt is not null)
        {
            return ChallengeConsumeOutcome.TooManyAttempts;
        }

        if (challenge.ConsumedAt is not null)
        {
            return ChallengeConsumeOutcome.Consumed;
        }

        if (challenge.Purpose != expectedPurpose || now >= challenge.ExpiresAt)
        {
            return ChallengeConsumeOutcome.InvalidOrExpired;
        }

        if (!string.Equals(challenge.SecretHash, OpaqueSecretHasher.Hash(secret), StringComparison.Ordinal))
        {
            challenge.AttemptCount++;
            if (challenge.AttemptCount >= _lifecycle.Value.MaxChallengeAttempts)
            {
                challenge.LockedAt = now;
                await _db.SaveChangesAsync(cancellationToken);
                return ChallengeConsumeOutcome.TooManyAttempts;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return ChallengeConsumeOutcome.InvalidOrExpired;
        }

        challenge.ConsumedAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return ChallengeConsumeOutcome.Succeeded;
    }

    private void ValidatePassword(string password)
    {
        var policy = _passwordPolicy.Value;
        if (password.Length < policy.MinimumLength)
        {
            throw new ArgumentException("رمز از حداقل سیاست پیکربندی کوتاه‌تر است.", nameof(password));
        }

        if (policy.RequireLetterAndDigit && !(password.Any(char.IsLetter) && password.Any(char.IsDigit)))
        {
            throw new ArgumentException("رمز باید طبق سیاست پیکربندی حرف و رقم داشته باشد.", nameof(password));
        }
    }
}
