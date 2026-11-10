using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tooba.Order.Application.Admin.Operations.Ports;

/// <summary>
/// TB-259: the operator's physical packing choice for one compatible operational delivery group.
/// </summary>
/// <remarks>
/// Grouping is not physical packaging. Inside the SAME server-owned compatible group the operator may
/// deliberately pack the constituent member parcels into ONE common package, or keep independent
/// parcels. The choice is part of the immutable occurrence envelope and is persisted with it.
/// </remarks>
public enum OperationalGroupPackingMode
{
    /// <summary>Constituent parcels are consolidated into ONE common package (historical behaviour).</summary>
    Common = 0,

    /// <summary>Constituent parcels stay independent; no consolidated package is created.</summary>
    Separate = 1,
}

/// <summary>TB-259: canonical wire tokens of the packing choice.</summary>
public static class OperationalGroupPackingModes
{
    public const string Common = "common";
    public const string Separate = "separate";

    /// <summary>Parses the operator token; blank/unknown is refused instead of guessed.</summary>
    public static bool TryParse(string? token, out OperationalGroupPackingMode mode)
    {
        switch ((token ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "":
            case Common:
                mode = OperationalGroupPackingMode.Common;
                return true;
            case Separate:
                mode = OperationalGroupPackingMode.Separate;
                return true;
            default:
                mode = OperationalGroupPackingMode.Common;
                return false;
        }
    }

    /// <summary>Wire token of a mode.</summary>
    public static string Token(OperationalGroupPackingMode mode) =>
        mode == OperationalGroupPackingMode.Separate ? Separate : Common;
}

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
    IReadOnlyList<OperationalGroupOccurrenceMember> Members,
    /// <summary>
    /// TB-259: the packing choice belongs to the same immutable envelope, so a changed choice is a
    /// typed conflict rather than a silent re-allocation. Defaults to the legacy common package.
    /// </summary>
    OperationalGroupPackingMode PackingMode = OperationalGroupPackingMode.Common);

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
    IReadOnlyList<OperationalGroupOccurrenceAllocation> Allocations,
    /// <summary>
    /// TB-259: packing choice read back from the durable envelope. A legacy row without a recorded
    /// choice means the historical common-package semantics — never a reinterpretation.
    /// </summary>
    OperationalGroupPackingMode PackingMode = OperationalGroupPackingMode.Common);

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

    /// <summary>
    /// TB-259: records a completed SEPARATE packing occurrence. It deliberately carries no package id:
    /// a completed separate operation is complete by its own exact constituent links, and its
    /// <c>ConsolidatedPackageId</c> staying null is the recorded result — never evidence of incompleteness.
    /// </summary>
    Task RecordCompletedSeparateAsync(
        Guid occurrenceId,
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

    /// <summary>
    /// Canonical JSON of the whole envelope including the TB-259 packing choice.
    /// </summary>
    /// <remarks>
    /// The choice is emitted ONLY for <see cref="OperationalGroupPackingMode.Separate"/>, so a common
    /// mode envelope is byte-identical to a TB-253-R4 envelope and every already recorded occurrence
    /// keeps its exact historical fingerprint and replays unchanged. Legacy rows therefore mean
    /// common-package semantics explicitly rather than by reinterpretation.
    /// </remarks>
    public static string CanonicalEnvelopeJson(OperationalGroupOccurrenceEnvelope envelope)
    {
        var head = CanonicalJson(envelope);
        if (envelope.PackingMode != OperationalGroupPackingMode.Separate)
        {
            return head;
        }

        // The serialized object always ends with '}', so appending one member keeps valid JSON and a
        // stable property order.
        return head[..^1] + ",\"packingMode\":\"" + OperationalGroupPackingModes.Separate + "\"}";
    }

    /// <summary>TB-259: reads the recorded packing choice; a legacy row means the common package.</summary>
    public static OperationalGroupPackingMode ReadPackingMode(string? envelopeJson)
    {
        if (string.IsNullOrWhiteSpace(envelopeJson))
        {
            return OperationalGroupPackingMode.Common;
        }

        try
        {
            using var document = JsonDocument.Parse(envelopeJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("packingMode", out var value)
                && value.ValueKind == JsonValueKind.String
                && string.Equals(
                    value.GetString(),
                    OperationalGroupPackingModes.Separate,
                    StringComparison.OrdinalIgnoreCase))
            {
                return OperationalGroupPackingMode.Separate;
            }
        }
        catch (JsonException)
        {
            // A malformed historical envelope cannot claim a separate result.
        }

        return OperationalGroupPackingMode.Common;
    }

    /// <summary>Stable SHA-256 fingerprint of <see cref="CanonicalEnvelopeJson"/>.</summary>
    public static string Fingerprint(OperationalGroupOccurrenceEnvelope envelope) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalEnvelopeJson(envelope))))
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
