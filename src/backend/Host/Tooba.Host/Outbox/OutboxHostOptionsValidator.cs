using Microsoft.Extensions.Options;

namespace Tooba.Host.Outbox;

/// <summary>
/// Fail-fast startup validation for Outbox deployment options.
/// </summary>
internal sealed class OutboxHostOptionsValidator : IValidateOptions<OutboxHostOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OutboxHostOptions options)
    {
        if (options.PollIntervalSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("Tooba:Outbox:PollIntervalSeconds must be greater than zero.");
        }

        if (options.BatchSize <= 0)
        {
            return ValidateOptionsResult.Fail("Tooba:Outbox:BatchSize must be greater than zero.");
        }

        if (options.RetryBaseDelaySeconds <= 0)
        {
            return ValidateOptionsResult.Fail("Tooba:Outbox:RetryBaseDelaySeconds must be greater than zero.");
        }

        if (options.MaxAttempts <= 0)
        {
            return ValidateOptionsResult.Fail("Tooba:Outbox:MaxAttempts must be greater than zero.");
        }

        if (options.LockSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("Tooba:Outbox:LockSeconds must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
