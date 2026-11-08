using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tooba.Order.Application.Admin.Operations.Ports;

/// <summary>TB-253-R4: one reviewed line quantity inside an immutable occurrence envelope.</summary>
public sealed record OperationalGroupOccurrenceSelection(Guid OrderLineId, decimal Quantity);

/// <summary>TB-253-R4: one constituent accepted group inside an immutable occurrence envelope.</summary>
public sealed record OperationalGroupOccurrenceMember(
    Guid ShippingGroupId,
    IReadOnlyList<OperationalGroupOccurrenceSelection> Selections,
    IReadOnlyList<Guid> ReuseShipmentIds);

/// <summary>
/// TB-253-R4: the COMPLETE immutable request of one Marketplace operational-group workflow.
/// </summary>
/// <remarks>
/// Occurrence identity is separate from any payload fingerprint: the same envelope under the same
/// occurrence is a replay, a different envelope under the same occurrence is a typed intent conflict,
/// and the same payload under a NEW occurrence is a lawful new operation with its own parcels.
/// </remarks>
public sealed record OperationalGroupOccurrenceEnvelope(
    Guid OccurrenceId,
    Guid CheckoutId,
    Guid ActorUserId,
    string OperationalGroupKey,
    string AcceptedMethodCode,
    IReadOnlyList<OperationalGroupOccurrenceMember> Members);

/// <summary>TB-253-R4: exact durable constituent link of one occurrence member.</summary>
public sealed record OperationalGroupOccurrenceAllocation(
    Guid ShippingGroupId,
    IReadOnlyList<Guid> CreatedShipmentIds,
    IReadOnlyList<Guid> ReusedShipmentIds);

/// <summary>TB-253-R4: durable occurrence state read back for replay/reconciliation.</summary>
public sealed record OperationalGroupOccurrenceRecord(
    Guid OccurrenceId,
    Guid CheckoutId,
    Guid ActorUserId,
    string OperationalGroupKey,
    string EnvelopeFingerprint,
    string EnvelopeJson,
    bool Completed,
    Guid? ConsolidatedPackageId,
    string? PackageNumber,
    string? ShippingMethodCode,
    IReadOnlyList<OperationalGroupOccurrenceAllocation> Allocations);

/// <summary>
/// Durable claim/read port for the immutable occurrence envelope and its exact constituent links.
/// </summary>
public interface IOrderOperationalGroupOccurrenceStore
{
    /// <summary>
    /// Durably claims the exact envelope before any allocation, or returns the already recorded one.
    /// The caller must compare equality itself and refuse a changed envelope with a typed conflict.
    /// </summary>
    Task<OperationalGroupOccurrenceRecord> ClaimAsync(
        OperationalGroupOccurrenceEnvelope envelope,
        string envelopeFingerprint,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records one member allocation exactly once. A concurrent twin receives the SAME recorded link
    /// instead of overwriting it.
    /// </summary>
    Task<OperationalGroupOccurrenceAllocation> RecordMemberAllocationAsync(
        Guid occurrenceId,
        OperationalGroupOccurrenceAllocation allocation,
        CancellationToken cancellationToken);

    /// <summary>Records the common package result once.</summary>
    Task RecordCompletedAsync(
        Guid occurrenceId,
        Guid consolidatedPackageId,
        string packageNumber,
        string shippingMethodCode,
        CancellationToken cancellationToken);

    /// <summary>Reads one occurrence (authority is re-checked by the caller).</summary>
    Task<OperationalGroupOccurrenceRecord?> GetAsync(
        Guid occurrenceId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Canonical envelope serialization/fingerprint. Deterministic regardless of client member/reuse order.
/// </summary>
public static class OperationalGroupOccurrenceEnvelopeCodec
{
    /// <summary>Canonical JSON of the whole envelope (stable member/line/reuse ordering).</summary>
    public static string CanonicalJson(OperationalGroupOccurrenceEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var members = envelope.Members
            .OrderBy(m => m.ShippingGroupId)
            .Select(m => new
            {
                shippingGroupId = m.ShippingGroupId.ToString("N"),
                selections = m.Selections
                    .OrderBy(s => s.OrderLineId)
                    .Select(s => new
                    {
                        orderLineId = s.OrderLineId.ToString("N"),
                        quantity = FormatQuantity(s.Quantity),
                    })
                    .ToArray(),
                reuseShipmentIds = m.ReuseShipmentIds
                    .Distinct()
                    .OrderBy(id => id)
                    .Select(id => id.ToString("N"))
                    .ToArray(),
            })
            .ToArray();

        return JsonSerializer.Serialize(new
        {
            checkoutId = envelope.CheckoutId.ToString("N"),
            occurrenceId = envelope.OccurrenceId.ToString("N"),
            actorUserId = envelope.ActorUserId.ToString("N"),
            operationalGroupKey = envelope.OperationalGroupKey,
            acceptedMethodCode = envelope.AcceptedMethodCode,
            members,
        });
    }

    /// <summary>Stable SHA-256 fingerprint of <see cref="CanonicalJson"/>.</summary>
    public static string Fingerprint(OperationalGroupOccurrenceEnvelope envelope) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson(envelope))))
            .ToLowerInvariant();

    /// <summary>Invariant quantity text, so 2m and 2.0m can never differ by serialization accident.</summary>
    public static string FormatQuantity(decimal quantity) =>
        quantity.ToString("0.############################", CultureInfo.InvariantCulture);
}

/// <summary>Serializes exactly the recorded shipment-id arrays for durable member links.</summary>
public static class OperationalGroupOccurrenceAllocationJson
{
    /// <summary>JSON array of ids (already distinct/ordered).</summary>
    public static string Serialize(IReadOnlyList<Guid> ids) =>
        JsonSerializer.Serialize(ids.Select(id => id.ToString("D")).ToArray());

    /// <summary>Reads a stored JSON array; malformed/legacy text yields an empty set.</summary>
    public static IReadOnlyList<Guid> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var raw = JsonSerializer.Deserialize<string[]>(json);
            if (raw is null)
            {
                return [];
            }

            var result = new List<Guid>(raw.Length);
            foreach (var value in raw)
            {
                if (Guid.TryParse(value, out var id) && id != Guid.Empty && !result.Contains(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
