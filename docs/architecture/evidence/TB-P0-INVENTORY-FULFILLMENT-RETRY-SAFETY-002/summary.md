# TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002 — summary

| Field | Value |
|---|---|
| Task | `TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002` (parent `TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001`) |
| Program | TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY |
| Mode | BOUNDED_ARCHITECTURE_GATE_AND_SAFE_REPAIR |
| Starting SHA | `04e3aa79f1fcf26c2c4c17f10ea1d3fce99dac91` (== `origin/main` == `Expected-Starting-HEAD`) |
| Final SHA | docs-only commit in this task (see the worker Result envelope / `git log -1`) |
| Verdict | **INCOMPLETE — BLOCKED_ARCHITECT_DECISION** |
| Production code changed | **NONE** (analysis + decision only) |
| Changed files | `docs/ai/tasks/TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002.task.md`, `docs/architecture/evidence/TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002/{boundary-map,failure-matrix,contract-decision,targeted-test-results,summary}` — docs only |

## Gate verdicts

| Gate | Verdict | One-line reason |
|---|---|---|
| A — ownership / persistence boundary trace | **PASS (trace complete)** | Fulfillment owns one local commit; Inventory owns a separate commit and, inside it, an autocommitted `ExecuteUpdate` that is not atomic even with its own reservation row. Details: `boundary-map.md`. |
| B — idempotency proof (same consumer, no double deduction, conflicting consumers rejected) | **BLOCKED** | No consumer/owner identity exists on a reservation and none is carried by the port; `ExternalReference` is order-process-wide (live evidence: all 5 member-1 reservations share it); the only key (`IdempotencyKey`) is cleared by `MoveTo`; no per-reservation consumption journal exists; the guarded `ExecuteUpdate` can deduct **twice** before `MoveTo` throws. 3 of 5 required proofs are impossible on current data/contract. |
| C — failure-window model | **PASS (model complete, defects confirmed)** | 8 windows documented in `failure-matrix.md`; W1 (stuck package), W7 (silent extra deduction), W8 (deduction not atomic with the reservation row) and W6 (unconditional skip for `Dispatched` members) are real defects. |
| D — smallest architecturally valid approach | **BLOCKED_ARCHITECT_DECISION** | Every provable fix needs at least one of: Inventory **schema migration** (consumption journal), **cross-module public contract change** (`IFulfillmentInventoryLifecyclePort` consumer key / status read), or **new business semantics** (replay acceptance). The only non-schema, non-contract option is the blanket `Consumed`⇒success shortcut, which Gate B explicitly forbids. `contract-decision.md` records the rejected options (A–D, F) and the recommended bounded design (E). |
| E — deploy parent before closing this gate? | **GO (constrained)** | Parent strictly narrows the failure window (all validation now precedes any write, single Fulfillment commit) and does not touch `ConsumeAsync`. Constraints: the Gate-C runbook may be used **once** for the currently-`Held` member-2 reservation; **HOLD** any retry of a dispatch whose reservation is already `Consumed`; no live action taken by this task. |

## Required Architect decision (precise)

1. Approve (or replace) design **E**: an Inventory-owned `reservation_consumptions` journal keyed by `ReservationId` (PK = concurrency primitive), written in the **same** transaction as the deduction and the `reservations.Status` compare-and-set, with typed `consume_conflict` for “journal missing / mismatched consumer” instead of silent success.
2. Approve the compatible extension of `IFulfillmentInventoryLifecyclePort.ConsumeAsync` with a consumer key (e.g. `fulfillment:package:{packageId}:member:{shipmentId}:line:{orderLineId}`) so “same logical fulfillment/shipment” becomes verifiable rather than assumed.
3. Confirm the replay semantics change to `ConsumeAsync` is acceptable business semantics (today the terminal state is deliberately not resurrectable; `MoveTo`, plus three durable tests, pin that invariant).
4. Decide the W6 remedy: reconcile `Dispatched` members whose reservation is still `Held`/not consumed instead of the unconditional `Consume = false` skip.
5. Migration ownership/sequencing for the new Inventory table (no live migration was attempted).

## Evidence index

* `boundary-map.md` — Gate A: call chain with file:line, reservation identity/ownership, existing idempotency facilities.
* `failure-matrix.md` — Gate C: 8 failure windows + read-only live-data corroboration + residual P0 risks.
* `contract-decision.md` — Gate B/D/E: proof table, locked invariants, options A–F, recommended design + required tests, deployment GO with constraints.
* `targeted-test-results.txt` — focused execution evidence.

## Validation executed

* `dotnet test Tooba.Inventory.Tests` → **Passed 13 / Failed 0** (exit 0), isolated artifacts path (running Host PID 15068 locks default outputs).
* `dotnet test Tooba.Host.Tests --filter PaidOrderReservationLifecycleTests|InventoryFoundationTests` → **Passed 13 / Failed 0** (exit 0).
* No new tests authored (blocked scope ⇒ no speculative production change).
* `git status` after the run: only the docs added by this task (no source files touched).

## Remaining P0 risk after this task

Unchanged and still open: a Fulfillment-commit failure after a successful Inventory consumption leaves the consolidated package permanently undispatchable (`inventory.reservation.not_active` on retry) **and can silently double-deduct stock** (W7), because the deduction is not atomic with the reservation status and no consumption journal/consumer identity exists. The parent repair is deployable (GO), but the cross-module replay gap stays open until design (E) — or an approved equivalent — is implemented.

## Runbook constraints (unchanged, restated)

* Never retry a dispatch whose reservation is already `Consumed` before design (E) lands.
* The single-shot Gate-C runbook for `MP-01A12F796EF1` is safe **only** while member 2's reservation is `Held` (verified read-only: `Held`, qty 5, `expires_at = null`, position `on_hand 200 / reserved 5`).
* No live writes, no migrations, no restarts were performed by this task.
