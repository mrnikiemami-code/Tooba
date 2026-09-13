using Microsoft.EntityFrameworkCore;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// ورود OTP مشتری روی User و چالش موجود Identity. فروشگاه موازی ساخته نمی‌شود.
/// </summary>
public sealed class IdentityOtpLoginService : IIdentityOtpLoginService
{
    private readonly IdentityDbContext _db;
    private readonly IOtpChallengeService _otp;
    private readonly IIdentityAuthenticationService _auth;

    /// <summary>سرویس ورود OTP را به schema Identity وصل می‌کند.</summary>
    public IdentityOtpLoginService(
        IdentityDbContext db,
        IOtpChallengeService otp,
        IIdentityAuthenticationService auth)
    {
        _db = db;
        _otp = otp;
        _auth = auth;
    }

    /// <inheritdoc />
    public async Task<OtpChallengeHandle> RequestLoginAsync(string mobile, CancellationToken cancellationToken)
    {
        var (_, normalized) = LoginIdentifierNormalizer.Normalize(LoginIdentifierKind.Phone, mobile);
        return await _otp.IssueAsync(OtpPurpose.Login, normalized, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthenticationResult> CompleteLoginAsync(
        string mobile,
        Guid challengeId,
        string oneTimeCode,
        CancellationToken cancellationToken)
    {
        string normalized;
        try
        {
            (_, normalized) = LoginIdentifierNormalizer.Normalize(LoginIdentifierKind.Phone, mobile);
        }
        catch (ArgumentException)
        {
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        var verified = await _otp.VerifyAsync(
            new OtpChallengeHandle { ChallengeId = challengeId, Purpose = OtpPurpose.Login },
            oneTimeCode,
            cancellationToken);
        if (!verified)
        {
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        var challenge = await _db.Challenges.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChallengeId == challengeId, cancellationToken);
        if (challenge is null
            || !string.Equals(challenge.IdentifierHash, OpaqueSecretHasher.Hash(normalized), StringComparison.Ordinal))
        {
            return AuthenticationResult.Fail(AuthenticationOutcome.InvalidCredentials);
        }

        var userId = await _auth.FindUserIdByIdentifierAsync(LoginIdentifierKind.Phone, normalized, cancellationToken);
        if (userId is null)
        {
            var now = DateTimeOffset.UtcNow;
            var user = UserAccount.Register(LoginIdentifierKind.Phone, mobile, now);
            var identifier = user.Identifiers[0];
            identifier.VerificationState = IdentifierVerificationState.Verified;
            identifier.VerifiedAt = now;
            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            userId = user.UserId;
        }
        else
        {
            var owned = await _db.Identifiers.FirstAsync(
                x => x.UserId == userId && x.Kind == LoginIdentifierKind.Phone && x.NormalizedValue == normalized,
                cancellationToken);
            if (owned.VerificationState != IdentifierVerificationState.Verified)
            {
                owned.VerificationState = IdentifierVerificationState.Verified;
                owned.VerifiedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return await _auth.EstablishSessionForUserAsync(userId.Value, cancellationToken);
    }
}
