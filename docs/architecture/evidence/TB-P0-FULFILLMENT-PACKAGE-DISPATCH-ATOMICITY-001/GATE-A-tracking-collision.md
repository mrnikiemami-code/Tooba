# GATE A — Deterministic generated member tracking-reference collision (F1)

Verdict: **PASS (FIXED, proven deterministically; no external carrier/DB contract change required).**

## 1. Collision confirmed as deterministic, not accidental

Two UUIDv7 identifiers carry a 48-bit millisecond timestamp in their most-significant bits.
The **first 32 bits** of the value are the top 32 bits of that millisecond timestamp, i.e. they
change only about every `2^16 ms ≈ 65.5 s`. Consequently **every UUIDv7 identifier generated
inside the same ~65 s window shares the same first eight hex characters.**

The pre-repair generator used exactly those eight characters:

```csharp
var baseCode = package.TrackingReference!.Trim();
var memberCode = $"{baseCode}-{member.ShipmentId.ToString("N")[..8].ToUpperInvariant()}";
```

Therefore the generated code collided **as a rule**, not by bad luck, whenever a consolidated
package had two or more members without their own tracking reference.

Live production/tenant evidence (read-only, database `tooba_alpha`, table `fulfillment`):

```
consolidated_package_id | package_number  | status  | tracking_reference
01a12f79-6ef1-7000-8578-dd7cc841195d | MP-01A12F796EF1 | Created | 125444

shipment_id                          | status     | tracking_reference
01a12f79-1ad4-7000-9174-3237c0d645bf | Dispatched | 125444-01A12F79
01a12f79-33ed-7000-b7ad-e562dcf67be8 | Created    |

first eight hex characters of both shipment ids: 01A12F79   <-- identical (root cause)
```

Member 1 was persisted as `Dispatched` with `125444-01A12F79`; member 2 then produced the
**same** code, hit the global unique guard and threw `fulfillment.tracking.duplicate`.

## 2. Enforcement points that made the duplicate fatal

| Layer | Location | Effect |
| --- | --- | --- |
| Directory pre-check | `FulfillmentDirectory.AssignTrackingCoreAsync` | `AnyAsync(... TrackingReference.ToLower() == normalized.ToLower())` → `ContractOperationException("fulfillment.tracking.duplicate")` |
| Database | `FulfillmentDbContext.Shipments`: `HasIndex(x => x.TrackingReference).IsUnique().HasFilter("tracking_reference IS NOT NULL")` | second write would fail with `DbUpdateException` → same code |
| Column contract | `shipments.tracking_reference` `HasMaxLength(128)`; `consolidated_packages.tracking_reference` `HasMaxLength(128)` | generated member code must stay ≤ 128 chars |

No carrier/DB contract says the member suffix must be eight characters: nothing in the
repository parses the generated member reference (searched for `TrackingReference.Substring`,
`TrackingReference.Split`, and suffix parsing — zero matches), and the length budget is the only
hard constraint. So the fix did **not** require changing the external contract.

## 3. Smallest deterministic collision-resistant reference

```csharp
internal static string BuildMemberTrackingReference(string packageTrackingReference, Guid shipmentId)
{
    var baseCode = packageTrackingReference.Trim();
    var suffix = shipmentId.ToString("N").ToUpperInvariant();       // 32 hex chars, unique per shipment
    var maxBaseLength = MaxTrackingReferenceLength - suffix.Length - 1;  // 128 - 32 - 1 = 95
    if (maxBaseLength < 0)
    {
        maxBaseLength = 0;
    }

    if (baseCode.Length > maxBaseLength)
    {
        baseCode = baseCode[..maxBaseLength];
    }

    return baseCode.Length == 0 ? suffix : $"{baseCode}-{suffix}";
}
```

Why this is a **proof**, not a heuristic:

* the suffix is the **full** 128-bit shipment identity (`Guid.ToString("N")`, 32 hex chars);
* member shipment ids inside one package are distinct by primary key, so their full-`N`
  representations are distinct → the generated codes are distinct;
* the base prefix is identical for every member of one package (and trimmed to the same fixed
  length for all of them), so it cannot mask or create a difference in the suffix;
* total length is bounded to `MaxTrackingReferenceLength == 128`, matching the column ceiling;
* `-` is only added when a non-empty base remains, so no leading separator is produced.

Prefix-boundary edge case covered: a 128-character central code is trimmed to 95 characters and
the remaining 32 unexpected characters are the shipment identity — uniqueness is preserved
because every member is trimmed by the same rule.

## 4. Backwards compatibility / no rewrite of existing codes

* The existing reference is **never** rewritten: a code is generated only when
  `string.IsNullOrWhiteSpace(shipment.TrackingReference)` and the package carries a central code.
* An already-`Dispatched`/`InTransit`/`Delivered` member is skipped before any mutation, so the
  live `125444-01A12F79` row cannot be touched by the repaired path.
* A package without a central tracking reference keeps the previous behaviour exactly
  (`fulfillment.dispatch.tracking_required` from `Shipment.EnsureDispatched`).

## 5. Focused tests (all passing, raw output in `focused-package-dispatch-tests.txt`)

`src/backend/Host/Tooba.Host.Tests/ConsolidatedPackageTests.cs` (real PostgreSQL via Testcontainers):

| Test | What it proves |
| --- | --- |
| `Package_dispatch_generates_collision_free_member_tracking_for_uuidv7_prefix_twins` | asserts the two seeded UUIDv7 ids share the first eight hex characters (precondition made explicit), then both members receive **distinct** full-identity codes, both become `Dispatched`, and an idempotent repeat does not rewrite the codes |
| `Package_dispatch_preserves_pre_assigned_member_tracking` | members that already carry a reference keep it byte-for-byte through package dispatch (no rewrite) |

No assertion depends on randomness: the shared prefix is asserted as a precondition of UUIDv7
semantics (same ~65 s window), and all expected codes are computed from the actual shipment ids.

## 6. Residual risk / limitation

* The generated format changed from `<base>-XXXXXXXX` to `<base>-<32 hex>`. Cosmetic only; no
  consumer parses it. Existing rows keep the old format permanently (intentional, no rewrite).
* The global unique index is still **not tenant/store scoped**. That is a separate finding and was
  deliberately **not** changed here, because the task forbids arbitrary tenant scoping of
  uniqueness.
