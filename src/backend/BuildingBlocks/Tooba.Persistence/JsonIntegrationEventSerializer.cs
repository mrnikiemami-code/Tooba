using System.Text.Json;
using System.Text.Json.Serialization;
using Tooba.BuildingBlocks;

namespace Tooba.Persistence;

/// <summary>
/// JSON با camelCase و type map ماژول‌ها. هیچ TypeNameHandling یا GetType آزاد روی payload نیست.
/// </summary>
public sealed class JsonIntegrationEventSerializer : IIntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyList<IOutboxModuleRegistration> _modules;

    /// <summary>
    /// serializer را به ثبت‌های ماژول وصل می‌کند.
    /// </summary>
    public JsonIntegrationEventSerializer(IEnumerable<IOutboxModuleRegistration> modules)
    {
        _modules = modules.ToArray();
    }

    /// <inheritdoc />
    public string SerializePayload(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);
    }

    /// <inheritdoc />
    public IIntegrationEvent Deserialize(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var clr = _modules
            .Select(m => m.ResolveEventClrType(message.EventType))
            .FirstOrDefault(t => t is not null)
            ?? throw new InvalidOperationException("Unknown integration event type mapping.");

        var instance = JsonSerializer.Deserialize(message.Payload, clr, Options) as IIntegrationEvent
            ?? throw new InvalidOperationException("Integration event payload could not be read.");

        var metadataProperty = clr.GetProperty(nameof(IIntegrationEvent.Metadata));
        metadataProperty?.SetValue(instance, MetadataFromColumns(message));
        return instance;
    }

    /// <summary>
    /// فراداده را فقط از ستون‌های Outbox می‌سازد تا payload نتواند Tenant را جعل کند.
    /// </summary>
    public static EventMetadata MetadataFromColumns(OutboxMessage message)
    {
        if (!Enum.TryParse<ToobaEdition>(message.Edition, ignoreCase: true, out var edition))
        {
            edition = ToobaEdition.Unset;
        }

        return new EventMetadata(
            EventId: message.Id,
            OccurredAt: message.OccurredAt.ToDateTimeOffset(),
            EventType: message.EventType,
            CorrelationId: message.CorrelationId,
            Version: message.Version,
            TenantId: message.TenantId,
            DeploymentId: message.DeploymentId,
            Edition: edition);
    }
}
