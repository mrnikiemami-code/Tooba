# TB-262-R1 — pending replacement resume, association-specific cancel, atomic non-expiring hold

Status: **PASS within the issued repair scope** — decision
`PENDING_REPLACEMENT_RESUMES_THROUGH_OWNING_EXECUTION_ASSOCIATION_SPECIFIC_IDEMPOTENT_CANCEL_AND_ATOMIC_NON_EXPIRING_ORDER_HOLD`.

This is NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "all shipping problems fixed", and NOT refund/financial automation.
It repairs only the three seams named in `262/ARCHITECT-REVIEW.md`. The accepted 262 direction (source-scoped damaged decision,
separate healthy-stock reservation, distinct replacement parcel, damaged stock never usable, cancellation retains the order
hold, existing UI/Toast, current authority) is preserved unchanged.

- Expected base: `a67e514ead76a058788add1be571512791db6646`
- Code commit: `e6cb0378edaf0986c8f7c1f656494ed6e8abc569`
- Evidence commit / merge: see `result-contract.txt`
- Parent 262 product: `f1ce02d7d5d2c5b6623af22c31b5e0eaef943cf2` (preserved as history, plus the 2026-11-11 retention revision)

---

## 1. A — a RECORDED decision is not a COMPLETED workflow

**Defect.** `AdminOrderOperationsOrchestrator.RunDamagedResolutionAsync` answered `recorded.Snapshot` for ANY
`Recorded` intent. `Recorded` covers three very different states: a durable no-stock decision, a `Reserved` decision
whose parcel write never landed, and a `Fulfilled` decision that owns a live parcel. The public retry therefore looked
successful with no parcel, consumed the damaged unit, vanished from the fresh projection and cleared the retained UI
envelope.

**Repair (Order owns the public command).**
`RunDamagedResolutionAsync` now separates *intent identity* from *workflow completion*:

- `completed` is ONLY `Status == Fulfilled` or `Outcome == Referral`. Those answer the exact recorded snapshot with zero writes
  (a referral is still "recorded, awaiting handling", never financial or delivery completion).
- every other `Recorded` state (no-stock decision, `Reserved` without a live parcel, parcel cancelled before dispatch)
  **continues** through `ResolveDamagedGoodsCoreAsync` with the current actor/checkout/fulfillment/parcel relationship,
  i.e. the production owning execution — no second decision, no second reservation, no silent success.
- `ResolvedCancelled` stays historical, `Conflict` stays a typed `order.operation.invalid`-family refusal, and a fresh
  projection that no longer offers the action cannot block a recorded continuation (the gate is skipped only for the
  exact recorded intent; an absent record still goes through the ordinary `EnsureProjectedActionAllowedAsync` path).

`FulfillmentDirectory.Damaged.cs` is the one owner of the continuation ladder and now documents it explicitly:
decision-first commit → own hold (`ReserveReplacementReservationAsync` when none, `FindReplacementReservationAsync` +
live-`Held` requirement when one exists, typed `fulfillment.damage.reservation_conflict` instead of a silent second hold)
→ linked parcel (attempt-scoped dispatch key, so a resumed attempt is genuinely new while an exact replay reuses the
recorded one).

**Server-owned resumability (no local hint needed).** `OccurrenceKey` is now exposed on
`DamagedResolutionSnapshot` / `DamagedGoodsResolutionRecord` (Contracts/Application), carried through
`AdminOrderDetailComposer` → `AdminShipmentView.DamagedResolutions` → `admin-api.ts`. Even after
`OutstandingDamagedQuantity` reached 0 and the fresh action disappeared, a reloaded operator page knows the exact
recorded occurrence it may continue.

**UI (existing dialog, no new surface).** `admin-order-operational-delivery-section.tsx` renders the durable decision
sentence per decision (`Reserved` → «موجودی سالم برای همین سفارش نگه داشته شده؛ مرسولهٔ جایگزین هنوز ساخته نشده است»,
`Recorded` → stock not yet secured, `Referral` → awaiting customer service, `Cancelled` → cancelled before dispatch) and
renames the ONE 44px entry to «ادامهٔ تعیین تکلیف کالای آسیب‌دیده» while a pending decision exists and
`receipt.outstanding === 0`. `post-dispatch-exception-dialog.tsx` gained `damagedResume`: the server-owned line,
quantity, outcome and occurrence key are replayed verbatim, the quantity field is not re-derived from leftover quantities,
and a resumed review cannot be replaced by a different one.

## 2. B — a stale cancellation cannot detach the current replacement

**Defect.** `FulfillmentDirectory.CancelShipmentAsync` always called `RetainReplacementStockForCancelledParcel`, and the
aggregate cleared `ReplacementShipmentId` / advanced `Attempt` without checking that the cancelled parcel was the
CURRENT linked replacement. Replaying the cancellation of an old, already cancelled parcel after a retry had created a
new live parcel erased the new link.

**Repair (Fulfillment owns the state transition).**
`DamagedGoodsResolution.RetainStockBeforeDispatch(Guid cancelledShipmentId, DateTimeOffset now)` returns immediately
unless `ReplacementShipmentId == cancelledShipmentId`. Only the first valid pre-dispatch cancellation of the CURRENT
parcel detaches that allocation and advances the parcel attempt once; a replay on the same already cancelled parcel is
a no-op, and a replay on an OLD parcel after a retry preserves the new link, the attempt, the held reservation and the
quantities. `FulfillmentUnit.RetainReplacementStockForCancelledParcel(replacementShipmentId, now)` forwards the exact
id. `Shipment.CancelPreDispatch` keeps its own idempotence (already `Cancelled` → no-op, dispatched/in-transit/delivered
→ typed refusal), the public actor/owner/package-lock guards are unchanged, and **no** Inventory release happens on a
parcel cancel. One live parcel per resolution remains enforced by the aggregate link plus the attempt-scoped dispatch
key unique index — no UI hiding is relied on.

## 3. C — the hold is non-expiring from its FIRST durable commit

**Defect.** `ReserveReplacementReservationAsync(... expiresAt: now + 7 days)` followed by a separate
`CommitReservationForPaidOrderAsync` left a crash window in which stock of an ACTIVE order could expire.

**Repair (Inventory owns the reservation; the caller cannot reintroduce a TTL).**
`DamagedReplacementReservationRequest` no longer carries an expiry at all, `InventoryDirectory.ReserveForDamagedReplacementAsync`
passes `expiresAt: null` into the existing atomic `ReserveAsync` primitive, `IFulfillmentInventoryGateway.ReserveReplacementReservationAsync`
lost its `expiresAt` parameter, and `FulfillmentDirectory.Damaged.cs` no longer performs a post-reserve commit step.
The purpose-bound hold (`damage-replacement:{resolutionId:N}`) is therefore non-expiring from its first durable commit,
which also closes the crash-between-reserve-and-link window: a crash leaves exactly ONE recoverable non-expiring hold
that the exact-key retry reuses. `ReleaseExpiredHoldsAsync` only selects `expires_at IS NOT NULL`, so it can never free
this stock while the order lives. Only an authorized ORDER cancellation can release the unconsumed hold once; the
unreachable post-dispatch whole-order cancellation branch and the 262 ordering limit are reported unchanged.

---

## 4. Proof (focused, real layers, one database)

All runs target the ONE canonical database `tooba_alpha` @ 127.0.0.1:5432 (SingleStore / `store-alpha`). Task-labelled
fixtures only; no user/screenshot row read or written; no raw business SQL, no manual DDL, no reset/reseed/backfill.

| Layer | Command | Result |
| --- | --- | --- |
| Fulfillment domain/behavior | `dotnet test ...Tooba.Fulfillment.Tests --filter DamagedResolution262Tests` | **8/8** (7 retained + `A_stale_cancel_replay_never_detaches_the_new_replacement_parcel`) |
| Fulfillment retained 258 | `--filter PostDispatchException` (same project, `TB258_CONNECTION`) | **36/36** |
| Host relational (real DB, real Inventory) | `dotnet test ...Tooba.Host.Tests --filter DamagedGoodsResolution262` (`TB262_CONNECTION`) | **8/8** (A/B/C/D/D2/E retained + new **F** stale-cancel replay and **G** atomic no-expiry) |
| Order public command (real DB) | `dotnet test ...Tooba.Order.Tests --filter AdminOrderPostDispatchReplay` (`TB258_CONNECTION`) | **11/11** (10 retained incl. H + new **I** pending-resume) |
| Frontend mounted | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts` | **54/54** (52 retained + 2 new R1 cases) |
| Frontend typecheck | `npx tsc --noEmit` | 0 errors |
| Frontend production build | `npm run build` | 0 errors, BUILD_ID `HH5Ghzd51vaj3oewQY1qF` |
| Solution build | `dotnet build Tooba.slnx -c Release` | 0 errors |

Public-path case **I** (`tb262r1-orchestrator-relational.txt`): first public attempt with a starved Inventory seam →
`fulfillment.damage.replacement_unavailable` with ZERO parcel; a retry in a NEW context is still the pending blocker
(not a success snapshot); the SAME occurrence then completes with its own hold and one parcel; cancelling that parcel
releases only the allocation (`ReleaseCalls == 0`) and leaves the decision pending; the retry resumes on the RETAINED
hold — a starved gateway proves `secondReserve=0` and the same `ReplacementReservationId` — with a genuinely NEW parcel;
replaying the OLD parcel's cancellation (`staleCancelReplay`) leaves the NEW parcel `Created`, the link and the hold
untouched; the exact public replay still answers that live parcel. Rows: `shipments3/decisions1`.

Host case **F**: cancel current P1 → retry creates P2 on the same hold → an INDEPENDENT context replays P1's
cancellation twice → the decision still links P2, status `Fulfilled`, one decision row, one live replacement hold,
`reserved=1`; the exact occurrence still answers P2.

Host case **G**: the owning Inventory seam is called DIRECTLY (no Fulfillment link exists yet) → the FIRST durable row
already has `expires_at = NULL`, status `Held`, one purpose-bound hold, `reserved=1`; the exact key retry returns the
SAME reservation; the reclaimer's selection predicate (`Held`, `expires_at IS NOT NULL`, `expires_at <= now + 400 days`)
matches **0** rows for it. The global batch job is deliberately not re-run against the shared canonical database.

Frontend R1 cases: a server-recorded pending decision resumes with its OWN occurrence key (`idempotencyKey ===
occurrenceKey`, durable envelope written BEFORE the POST, cleared only after authoritative completion) and the section
shows the durable `Reserved` sentence plus exactly one «ادامهٔ تعیین تکلیف کالای آسیب‌دیده» entry without claiming the
replacement parcel already exists.

## 5. Deploy

- **No migration was added by this repair and none was applied** (no schema change; `git status` shows no `Migrations`
  change). The parent 262 additive migration remains as recorded.
- Host republished to `artifacts/publish-primary` and restarted (previous PID `30928` stopped first): new PID **36116**,
  `/health` 200, `/health/ready` 200 → `SingleStore`, messaging `Healthy`.
- Frontend rebuilt and restarted: PID **30688**, BUILD_ID **`HH5Ghzd51vaj3oewQY1qF`**, `/fa` 200; served chunk
  `7825-cec5013e3aaa3dc7.js` carries «ادامهٔ تعیین تکلیف کالای آسیب‌دیده».
- ONE database `tooba_alpha`; no disposable/test/parallel/Docker DB, no 15433, no edition/connection/port/appsettings
  change; DataProtection and CapturedPreview unchanged (local ON / checked-in OFF); no provider call, no fake waybill;
  255 parked; live Tipax deferred.

## 6. Known limits

- A `Referral` stays an honest pending customer-service/financial item: money, Returns, Settlement and Payment are untouched.
- The authorized ORDER-cancellation release branch is still unreachable for an order that already dispatched a parcel
  (`fulfillment.cancel.already_dispatched`) and a damaged receipt necessarily comes from a shipped parcel — reported as a
  concrete ordering limit, unchanged by this repair; for the real scenario the hold simply stays with the active order.
- Parent consolidated-package damage/return orchestration remains unsupported and the TB-258 child lock is preserved.
- Concurrency/recovery are proven with independent real contexts + durable guards, not a stress test.
- No worker-run live user trial and no screenshot matrix; the UI proof is the mounted real section/dialog against real
  server facts.
- The first exploratory Host run of case G invoked the shared global reclaimer once with a +400-day clock (later run: 0
  rows released); the committed case evaluates the same predicate read-only and the ledger records both invocations.

STOP_FOR_ARCHITECT; automaticNext NONE.
