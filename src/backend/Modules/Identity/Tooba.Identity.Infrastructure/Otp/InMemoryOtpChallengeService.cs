using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Contracts;

namespace Tooba.Identity.Infrastructure.Otp;

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
