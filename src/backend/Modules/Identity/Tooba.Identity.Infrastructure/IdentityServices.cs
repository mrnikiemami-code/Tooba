using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// پیاده‌سازی موارد استفادهٔ Identity روی DbContext همین ماژول.
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
            throw new IdentityDuplicateIdentifierException(command.IdentifierKind, normalized);
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

    private void ValidatePassword(string password)
    {
        var policy = _policy.Value;
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

/// <summary>
/// یکتایی شناسهٔ نرمال در همان دامنهٔ پایگاه نقض شده است.
/// </summary>
public sealed class IdentityDuplicateIdentifierException : InvalidOperationException
{
    /// <summary>
    /// استثنا را با گونه و مقدار نرمال می‌سازد. مقدار نباید در لاگ عمومی تکرار شود.
    /// </summary>
    public IdentityDuplicateIdentifierException(LoginIdentifierKind kind, string normalizedValue)
        : base("شناسهٔ ورود نرمال در این دامنهٔ هویت تکراری است.")
    {
        Kind = kind;
        NormalizedValue = normalizedValue;
    }

    /// <summary>
    /// گونهٔ شناسه.
    /// </summary>
    public LoginIdentifierKind Kind { get; }

    /// <summary>
    /// مقدار نرمال تکراری.
    /// </summary>
    public string NormalizedValue { get; }
}

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

/// <summary>
/// OTP درون‌فرآیندی برای تست/dev. ارائه‌دهندهٔ SMS نیست.
/// </summary>
public sealed class InMemoryOtpChallengeService : IOtpChallengeService
{
    private readonly IOtpSender _sender;
    private readonly Dictionary<Guid, (OtpPurpose Purpose, string Code)> _challenges = [];
    private readonly object _gate = new();

    /// <summary>
    /// سرویس را با فرستندهٔ قابل تعویض می‌سازد.
    /// </summary>
    public InMemoryOtpChallengeService(IOtpSender sender) => _sender = sender;

    /// <inheritdoc />
    public async Task<OtpChallengeHandle> IssueAsync(OtpPurpose purpose, string destination, CancellationToken cancellationToken)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var id = UuidV7.New();
        lock (_gate)
        {
            _challenges[id] = (purpose, code);
        }

        await _sender.SendAsync(purpose, destination, code, cancellationToken);
        return new OtpChallengeHandle { ChallengeId = id, Purpose = purpose };
    }

    /// <inheritdoc />
    public Task<bool> VerifyAsync(OtpChallengeHandle handle, string oneTimeCode, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_challenges.TryGetValue(handle.ChallengeId, out var stored))
            {
                return Task.FromResult(false);
            }

            var ok = stored.Purpose == handle.Purpose && stored.Code == oneTimeCode;
            if (ok)
            {
                _challenges.Remove(handle.ChallengeId);
            }

            return Task.FromResult(ok);
        }
    }
}

/// <summary>
/// فرستندهٔ جعلی که کد را در حافظه نگه می‌دارد نه در لاگ.
/// </summary>
public sealed class CapturingOtpSender : IOtpSender
{
    /// <summary>
    /// آخرین کد برای تست؛ در تولید استفاده نشود.
    /// </summary>
    public string? LastCode { get; private set; }

    /// <summary>
    /// آخرین هدف.
    /// </summary>
    public OtpPurpose? LastPurpose { get; private set; }

    /// <inheritdoc />
    public Task SendAsync(OtpPurpose purpose, string destination, string oneTimeCode, CancellationToken cancellationToken)
    {
        LastPurpose = purpose;
        LastCode = oneTimeCode;
        return Task.CompletedTask;
    }
}

/// <summary>
/// ذخیرهٔ اتصال IdP روی جدول Identity.
/// </summary>
public sealed class EfExternalIdentityDirectory : IExternalIdentityDirectory
{
    private readonly IdentityDbContext _db;

    /// <summary>
    /// فهرست را روی DbContext ماژول می‌سازد.
    /// </summary>
    public EfExternalIdentityDirectory(IdentityDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task BindAsync(Guid userId, string issuer, string subject, CancellationToken cancellationToken)
    {
        _db.ExternalBindings.Add(new ExternalIdentityBinding
        {
            Id = UuidV7.New(),
            UserId = userId,
            Issuer = issuer.Trim(),
            Subject = subject.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> FindUserIdAsync(string issuer, string subject, CancellationToken cancellationToken)
    {
        var row = await _db.ExternalBindings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Issuer == issuer.Trim() && x.Subject == subject.Trim(), cancellationToken);
        return row?.UserId;
    }
}

/// <summary>
/// ذخیرهٔ MFA روی جدول Identity.
/// </summary>
public sealed class EfMfaEnrollmentStore : IMfaEnrollmentStore
{
    private readonly IdentityDbContext _db;

    /// <summary>
    /// فروشگاه را روی DbContext ماژول می‌سازد.
    /// </summary>
    public EfMfaEnrollmentStore(IdentityDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task EnrollAsync(Guid userId, MfaFactorKind factorKind, CancellationToken cancellationToken)
    {
        _db.MfaEnrollments.Add(new MfaFactorEnrollment
        {
            Id = UuidV7.New(),
            UserId = userId,
            FactorKind = factorKind,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MfaFactorKind>> ListEnabledAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.MfaEnrollments.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsEnabled)
            .Select(x => x.FactorKind)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// جمع‌آوری رخداد امنیتی در حافظه برای تست و درز Audit بعدی.
/// </summary>
public sealed class InMemoryIdentitySecurityEventSink : IIdentitySecurityEventSink
{
    private readonly List<IdentitySecurityEvent> _events = [];

    /// <summary>
    /// رخدادهای ثبت‌شده بدون راز.
    /// </summary>
    public IReadOnlyList<IdentitySecurityEvent> Events
    {
        get
        {
            lock (_events)
            {
                return _events.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public Task RecordAsync(IdentitySecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            _events.Add(securityEvent);
        }

        return Task.CompletedTask;
    }
}
