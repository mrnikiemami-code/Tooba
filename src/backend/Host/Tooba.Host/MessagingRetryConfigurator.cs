using MassTransit;

namespace Tooba.Host;

/// <summary>
/// سیاست retry/redelivery محدود برای consumerهای SQL Transport.
/// </summary>
internal static class MessagingRetryConfigurator
{
    /// <summary>
    /// 2 immediate + 3 delayed interval؛ بدون retry بی‌نهایت.
    /// </summary>
    internal static void ApplyConsumerRetry(IRetryConfigurator retry)
    {
        retry.Immediate(2);
        retry.Intervals(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30));
    }
}
