using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// ناشر بسته وقتی messaging خاموش است. پیام را دور نمی‌ریزد و به in-process برنمی‌گردد.
/// </summary>
internal sealed class MessagingDisabledPublisher : IIntegrationEventPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "Messaging is disabled. Enable Tooba:Messaging or the explicit Testing in-process double; silent in-process fallback is forbidden.");
    }
}
