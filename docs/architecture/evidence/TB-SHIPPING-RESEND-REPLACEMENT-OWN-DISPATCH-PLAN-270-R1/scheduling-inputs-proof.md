# TB-270-R1 — captured attempt scheduling inputs: what the seam reads, and what it refuses

Status: **implemented and proven.** This document records the exact mapping and refusal rules of the ONE additive
Shipping Contracts seam delivered by 270-R1. It is not a plan, not a feasibility check and not a claim that any attempt
can be scheduled: it only states which stored facts are returned, and which are not.

## 1. Why the read exists (inherited from 270, re-verified at the base)

A resend/replacement attempt needs its own week shapes to be scheduled. The Order-owned accepted evidence stores the
plan's **outputs**; the only pre-existing Contracts read returning the week shapes is the rate-quote lookup, whose request
requires a Destination plus optional measurements and whose selection (hence weeks) is null unless a rate row matched.
270-R1 therefore adds a read that needs **no destination, no measurement, no currency and no rate selection**.

The transport duration is not a payload-level fact: `MinDeliveryDays`/`MaxDeliveryDays` live inside
`Groups[].RateRows[]`. The request consequently carries the exact `GroupId` **and** `RateRowId`; nothing is inferred from
ranges, amounts, geography or array order.

## 2. Request identity (all required unless marked optional)

| Field | Rule |
| --- | --- |
| `OwnerKind` + `OwnerTenantId`/`OwnerSellerPartyId` | resolved by the existing `ShippingCapturedOwnerScopeResolver` against the current edition/tenant context; a partial or mismatching pair is `OwnerScopeUnavailable` |
| `DraftId`, `SourceRevision` | passed to the existing exact `GetCapturedRevisionAsync`. There is no newest/current/active fallback |
| `ExpectedServiceCode`, `ExpectedOptionCode` | compared with the STORED values using the same canonical trim + case-insensitive rule the captured quote read uses; an absent option is `null`, never `""` |
| `GroupId`, `RateRowId` | exact; a foreign row under a valid group is `RateRowUnavailable`, never a sibling row |
| `ExpectedSnapshotId` (optional) | when bound, must equal the row actually resolved |

## 3. What is returned, loss-lessly

| Stored fact | Source in the captured payload | Mapping rule |
| --- | --- | --- |
| provenance | snapshot row | snapshot/draft/revision/format/owner echoed as stored |
| service/option | payload root | canonical trimmed values |
| group + rate row ids | `Groups[].RateRows[]` | the exact resolved pair |
| `MinDeliveryDays`/`MaxDeliveryDays` | the SAME rate row | copied; never a sibling's, never a default |
| `PreparationMinutes`/`HandoffMinutes` | `MethodSettings` | `null` when absent — never 0 |
| `ProcessingMode` | `MethodSettings.PreparationMode` | `Unspecified` when absent — never `WorkingWeek` |
| receiving week + `SlotDurationMinutes` | `WeeklySchedule` | 7 stored days, local `[OpensAt, ClosesAt)`, closed day keeps its remembered hours; `null` slot duration stays `null` (full-day window) |
| carrier week | `CarrierAcceptance` | its OWN `TimeZoneId`, its OWN closed days |
| processing week | `ProcessingHours` | its OWN `TimeZoneId`; the three weeks are never collapsed |

Three separate timezones are preserved because they are three separate stored calendars (the fixture proves
`Asia/Tehran`, `Asia/Tehran`, `Asia/Dubai`).

`MissingInputs` carries stable input codes (`method_settings`, `preparation_handoff_minutes`, `processing_mode`,
`receiving_week`, `carrier_week`, `processing_week`). `HasCompleteInputs` means only "every input this seam declares is
stored"; it carries no feasibility, activation, coverage or eligibility meaning.

## 4. Refusals (typed, stable, always with NO payload)

| Verdict | Reason code | Trigger |
| --- | --- | --- |
| `OwnerScopeUnavailable` | `shipping.attempt_scheduling.owner_scope_unavailable` | owner kind/tenant/seller pair unresolvable in the current context |
| `CapturedRevisionUnavailable` | `…captured_revision_unavailable` | absent revision, another owner's capture, corrupt body, body headers disagreeing with the row |
| `SnapshotMismatch` | `…snapshot_mismatch` | caller-bound snapshot id differs from the resolved row |
| `ServiceOptionMismatch` | `…service_option_mismatch` | stored service/option differ after canonicalization |
| `GroupUnavailable` | `…group_unavailable` | requested group id absent inside the capture |
| `GroupAmbiguous` | `…group_ambiguous` | more than one stored group with that id |
| `RateRowUnavailable` | `…rate_row_unavailable` | requested row id absent inside that group |
| `RateRowAmbiguous` | `…rate_row_ambiguous` | more than one stored row with that id in that group |

Exceptions are never converted into a refusal: an unexpected dependency failure propagates with its own identity, and a
cancelled caller token propagates as `OperationCanceledException` with that token.

## 5. Read-only and boundary facts

* Reads the immutable capture row only; `AsNoTracking`; zero `SaveChanges`, zero capture, zero transaction.
* No Destination, measurement, weight, item count, currency, freight/packaging amount, coverage or Geography read; no
  rate selection; no provider/waybill call.
* No second scheduling engine: the existing `ShippingDispatchSchedulingAuthority` remains the only authority, and it is
  not called here.
* Registered once, scoped, in `ShippingModule`; **no caller** — Order/Fulfillment/queue/detail/frontend are untouched.
* No table, column, migration, command, endpoint, projection or frontend change in this round.

## 6. What this does NOT prove

That a real attempt can be scheduled, that the method is active, that the seller's destination is covered, that stock
exists, that a customer window is feasible, or that any parcel has or will get a plan. Those remain 270-R2 (Order-owned
plan + reviewed command + preview + queue/detail + review step), which is **NOT issued**.
