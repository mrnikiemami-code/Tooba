# First-send durability and lifecycle fence — proof (Repairs A and D)

All cases are MOUNTED-SURFACE evidence (`app/admin/admin-shipment-plan-270r2.mount.test.ts`, suite **14/14**), using
deterministic deferred promises — no sleeps, no timers, no retry loop.

## A1 — unresolved from the instant the request may be sent

Test: `TB-270-R4: the FIRST post is unresolved the instant it may be sent — an unmount before any catch recovers it exactly`

1. The operator selects a slot and confirms; the executor returns a promise that **never settles** (no catch, no
   `finally`, no epoch decision ever runs for it).
2. Inside the executor, the durable record is read back and asserted:
   - `occurrenceKey` equals the `idempotencyKey` about to be sent,
   - `requestBody` equals the exact serialized bytes of the request,
   - `uncertain === false` (nothing threw) **and** `unresolved === true` — the record is unresolved although no error
     was ever caught.
   This proves the envelope is durable **before** the executor can have produced a server fact.
3. The component unmounts while the POST is still open. The stored record is unchanged — an unmount can neither discard
   nor replace an unresolved intent.
4. A remount with a **different** freshly-offered calendar recovers the same body/key: the frozen banner is shown, the
   fresh `admin-shipment-plan-confirm` button does not exist (only `…-retry-same`), and the newer slot is rendered but
   cannot become a new intent.
5. The explicit retry sends **byte-identical** bytes (`retried[0] === posts[0]`); on the authoritative success the exact
   occurrence is compare-and-cleared and the surface closes once.

Root cause closed: R3 froze only `rec.uncertain && rec.requestBody`, and `uncertain` could only become true inside a
caught failure — which an unmount interrupts before it happens.

## A2 — failed preview and legacy records

Test: `TB-270-R4: an unresolved intent survives a FAILED preview, and a TB-270-R3 record is never assumed never-sent`

- A TB-270-R3-shaped record (exact body retained, `uncertain:false`, **no** `unresolved` field) is written directly to
  the same storage key the previous build used.
- The preview loader fails (`host-unreachable`), so there is no calendar at all.
- The surface still offers exactly one action — `…-retry-same` — and re-sends the legacy body verbatim. The record is
  therefore migrated conservatively: "no authoritative terminal result" is never read as "never sent".

`isUnresolvedShipmentPlanAttempt` = `Boolean(attempt && attempt.requestBody && attempt.unresolved !== false)`, and
`writeShipmentPlanAttempt` refuses to be durable unless a read-back equals the serialized record; the modal refuses to
POST when `written.durable === false`.

## A3 — conflict / authorization is not proof of "no write"

Test: `TB-270-R4: a conflict/authorization answer keeps the exact intent recoverable instead of assuming no write`

- The command answers `409 order.shipment_plan.conflict`. Because that code is deliberately NOT in
  `PROVEN_NO_WRITE_REFUSALS`, the surface stays frozen: no `prior-outcome` block, the record keeps `unresolved: true` and
  the identical body.
- The explicit retry repeats the identical bytes and no new key is minted; when the server then answers the exact replay,
  `clearShipmentPlanAttemptIfCurrent` removes that one occurrence and the surface completes.

Proven no-write refusals (`selection_expired`, `review_context_changed`, `not_created`, `first_dispatch_not_plannable`,
`payment_required`, `group_missing`, `binding_missing`, `binding_mismatch`, `service_missing`, `package_locked`,
`source_not_ready`, `source_quantity_mismatch`, `no_feasible_date`, `order.cancelled.blocks_action`,
`order.operation.invalid`, `shipping.scheduling.*`) still end the review, keep the prior outcome on screen and allow a
deliberate fresh review (retained TB-270-R3 case, suite-green).

## D1 — close/unmount/retarget invalidate the presentation epoch

- The effect now bumps `epochRef` in **both** branches: when `open` is false (close) and in the effect cleanup (unmount /
  retarget / `load` identity change). A stale answer can therefore never write the surface that replaced it.
- `send.finally` only clears `pending` when `epoch === epochRef.current`; every other state write in the success, refusal
  and thrown branches is behind the same epoch check.

Test: `TB-270-R4: a late answer after unmount/retarget neither clears a newer intent nor closes a newer surface`

- A first POST is started on parcel A, then the surface is **retargeted** to parcel B while the POST is open; a newer
  reviewed intent (`newer-key`) is recorded for A's scope.
- Releasing the old answer as `ok` produces: `onClose` count **0** (the replaced surface is not closed), B keeps its own
  calendar, and A's stored record is still `newer-key` — a late success for an older occurrence cannot clear newer storage.

## D2 — compare-and-clear / compare-and-update, not scope-only

`clearShipmentPlanAttemptIfCurrent(scope, occurrenceKey, requestBody)` clears only when the currently recorded occurrence
**and** body match. `markShipmentPlanAttemptFailedIfCurrent(scope, attempt, …)` likewise reaches storage only when the
recorded occurrence and body match, so an outcome arriving after a genuine remount or a replacement review records the
truth for **its own** occurrence and never overwrites the newer one. The same-tick `inFlightRef` guard and the exact
viewer/checkout/parcel scope are retained; no global timer, lock timeout or assumed request cancellation was introduced.
