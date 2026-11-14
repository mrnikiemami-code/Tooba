# TB-265-R1 worker result — durable Order closure, source-exact claim settlement, atomic partial release, money/disposition recovery, operator path

Status **PASS** within the issued A–E repair scope.
Decision **DURABLE_ORDER_OWNED_LINE_OBLIGATION_CLOSURE_SOURCE_AND_QUANTITY_EXACT_CLAIM_SETTLEMENT_ATOMIC_INVENTORY_PARTIAL_RELEASE_AND_INDEPENDENT_MONEY_VS_DISPOSITION_RECOVERY**.
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "shipping complete", NOT a live paid/refund/provider proof.

Base `d6bdc88c99502e115ebd3e3d4e7f64087dadc0f2` (verified ancestor of the work tree). Single worker, one task, no delegation, no historical agent resumed.
Code/evidence/HEAD SHAs are recorded in `result-contract.txt`.

## A — real Order-owned durable closure (was a read-only calculation)

- New Order-owned fact `OrderLineObligationClosureRecord` → `"order".order_line_obligation_closures` (additive migration
  `order 20261114120000_AddOrderLineObligationClosures`), with `closed_at_utc`, the return request id, the source claim id,
  the closed quantity and the approval identity.
- `OrderReturnObligationClosure` writes that fact inside the owning persistence seam: it locks the `"order".order_lines`
  row (`FOR UPDATE`, schema quoted because `order` is a reserved word), replays the exact recorded closure for the same
  case/claim/source/quantity, refuses a changed source or quantity with `closure_intent_conflict` **before** any write, and
  bounds the cumulative closed quantity by the still-owed obligation instead of subtracting the whole requested quantity.
  A sibling portion of the same line is never closed by another case.
- The relevant Order capability/obligation reader consumes the persisted row, so a later send/supply/recovery path sees the
  closed remainder; historical ordered/packed/shipped quantities are not decremented and the whole-order
  `already_dispatched` rule is untouched.
- Proven persisted: case G reads the row back **from an independent context** (`independentRead=true`) and a changed
  quantity is refused (`conflict=closure_intent_conflict`) with `reserved=1` unchanged.

## B — source/quantity-exact Fulfillment settlement and claim lifecycle

- New `CourierReturnClaimLineRecord` (unique per case + `ActiveKey = {receiptId:N}:{orderLineId:N}:{condition}`) and
  `CourierReturnClaimAllocation` (per allocated quantity: the cancelled replacement parcel and its **own** hold
  reservation), plus `CourierReturnClaimBinding`/`CourierReturnAllocationState` value objects and migration
  `fulfillment 20261114130000_AddCourierReturnClaimLinesAndAllocations`.
- Settlement is bound to the claim's own source parcel/receipt and the canonical per-line/per-condition quantity. Each
  claimed line receives its own amount from its own hold — there is no "first hold gets the total" shortcut and a case on
  source A can never cancel or release source B of the same line.
- A requested case freezes dispatch of the `Created` replacement it claimed
  (`fulfillment.returns.replacement_claimed_by_case`) through the owning agent state, so the claimed portion cannot also
  ship while the case is open; rejecting the `Requested` case restores only that claim to the source (stock stays `Held`).
  An already dispatched replacement can never be redirected/refunded as unused
  (`replacement_already_dispatched`, `releasedHeld=0`, reservation `Consumed`).
- An exact retry repairs a case whose claim→case link write was lost (`linkRepaired=true`, `duplicates=0`) instead of
  returning forever an unlinked snapshot.
- Proven relational: A (claim exactly once across both conditions), B (two independent contexts — one winner, one refusal),
  C (reject returns only its branch claim and the source is claimable again), E (recorded `Created` replacement redirected
  into the case and released once), F (dispatched replacement refused), H (requested case freezes dispatch; after reject it
  dispatches), I (lost-link repair).

## C — atomic Inventory partial release

- `InventoryReturnObligationRelease.ReleaseUnconsumedHeldAsync` now runs one atomic transaction with a row lock on
  `inventory.reservations`: the reservation's held quantity, the stock position `Reserved` decrement and the durable release
  identity are committed all or nothing. The physical unit `OnHand` never moves on a release.
- Same key + same reservation + same quantity replays the recorded amount (`Replayed`); a changed reservation/quantity is a
  conflict before any write; two concurrent keys serialize and can never release the same remaining held portion twice.
- Proven relational: J — held 3, two contexts release 2 and 1, total exactly 3, `reserved=0`, `onHand=10`, two durable
  release rows, exact replay `Replayed`, the original sale reservation stays `Consumed`.

## D — approval/rejection/refund/disposition without crossing money boundaries

- Courier-origin approval has a durable monotonic phase (`ObligationClosed` → `StockSettled` → `RefundProcessing` →
  `RefundReleased`); the Order closure and the stock settlement are separate owning phases, so a refund is not attempted
  before confirmed source/Order/stock closure.
- Money is tracked independently of physical disposition: a **successful** refund with a failing restock stays successful
  and the case keeps `StockSettled` with `Disposition.None`; `RetryRefundAsync` then repeats **only** the unfinished
  disposition with the same identity (`refundCalls=1`, one restock, final `SaleableRestock`,
  `phase=RefundReleased`). A confirmation is only written after the owning restock confirms, so a failed restock can never
  persist a false completed restock or drive an invalid `Completed → RefundFailed` transition.
- Reject only applies to a genuinely `Requested` case and releases the claim through the owning path; a late reject (case
  already refunding/partly closed) is a typed refusal with zero claim/stock/money mutation.
- Proven relational: K (money once, no false saleable restock, disposition-only retry) and D (approval closes the line,
  releases only the unconsumed held quantity once, restocks once, `onHand=7`, `reservation=Consumed`).

## E — operator path and localization

- The shipment admin read contract now projects the linked courier-origin cases (`CourierReturnCaseSnapshot` →
  `ShipmentSnapshot.CourierReturnCases` → `admin-api`), and the shared operational parcel card renders exactly ONE
  «مشاهدهٔ پروندهٔ مرجوعی» link per linked case (44px target, `data-testid=admin-order-post-dispatch-courier-case-…`), so
  the case link survives a reload and no longer exists only in the transient create response. The finance-free shipping
  view stays finance-free.
- The existing Returns detail renders the courier origin, the durable closure phase, the quantity released from the hold and,
  per item, the physical condition and the recorded disposition in Persian (`آسیب‌دیده` / `سالم`,
  `آسیب‌دیده — غیرقابل عرضه` / `سالم — بازگشت به موجودی فروش` / `در انتظار تعیین تکلیف`) — never a raw enum.
- `admin-error-map` gained the typed courier-return/claim reasons (`returns.courier_origin.*`, `fulfillment.returns.*`), so a
  known refusal is not surfaced as an unknown server error.
- The served production build carries the new copy: `BUILD_ID JD0cZnOk5evPmE-HmLmNl`, chunks
  `3852-a745543e9279487e.js` and `7825-e370aae29c6ec503.js` (HTTP 200) contain «مشاهدهٔ پروندهٔ مرجوعی»,
  «مرحلهٔ بستن پرونده» and `admin-order-post-dispatch-courier-case`.

## Focused proof (each check once)

- `Tooba.Host.Tests` `CourierOriginReturns265RelationalTests` on real `tooba_alpha`: **11/11** (A–K; recorded fixture ids in
  `tb265-relational.txt`, full log `tb265-behavior`/`tb265-relational`).
- `Tooba.Returns.Tests` `CourierOriginReturns265Tests`: **5/5**.
- `Tooba.Fulfillment.Tests` `CourierReturnClaim265Tests`: **8/8**.
- Frontend `npm run test:returns`: **7/7**; `npm run typecheck`: 0 diagnostics; `npm run build`: exit 0.
- Solution build: **0 errors** (8 pre-existing warnings, `NU1900` offline package-vulnerability and two pre-existing
  Order XML/default-parameter warnings); `dotnet publish` of the Host: 0 errors.

## Deploy / runtime

- Migrations applied by the repository `Tooba.MigrationRunner` only, after a private pre-migration backup
  `D:\temp\tb265-backup\tooba_alpha-pre-tb265-r1.dump` (3,457,390 bytes, 07:45 local; apply run 07:48).
  `plan --tenant store-alpha`: `order current=20261114120000_AddOrderLineObligationClosures pendingCount=0`,
  `fulfillment current=20261114130000_AddCourierReturnClaimLinesAndAllocations pendingCount=0`, every other module
  `pendingMigrations: []` (`tb265-plan.txt`). No manual DDL, no business SQL, no reset/reseed/backfill, no global reclaimer.
- ONE canonical database `tooba_alpha` @127.0.0.1:5432, SingleStore, tenant `store-alpha`.
- Host republished to `artifacts/publish-primary` (08:00) and running on 5088 (PID 28852, `/health` 200,
  `/health/ready` 200 → SingleStore).
- FE production server restarted on the new build: port 3000, PID 11200, `/fa` 200, BUILD_ID
  `JD0cZnOk5evPmE-HmLmNl`, previous build id 404.
- DataProtection and CapturedPreview untouched (local ON / checked-in OFF); 255 parked; Tipax deferred; accepted
  scheduling/mobile work untouched; no user/screenshot rows written.

## Known limits / disclosures

- No browser click-through was executed by the worker in this round: the operator path is proven by the owning read
  contracts (`CourierReturnCases` on the shipment read model, the Returns snapshot fields and their renderers), the served
  production build containing the exact copy, and the owning-application relational cases. A live operator
  create→reload→approve walkthrough in the browser remains for the user trial.
- Refund outcomes come from a task-owned controlled financial port (a recorder), not a real provider; no wallet credit or
  real money moved. The seam is disclosed, and the tests assert attempt identity/count rather than provider behaviour.
- The parent common-package return orchestration remains unsupported; the existing real child guards are unchanged.
- No whole-module/base-parity suite was run; only the focused 265 cases plus the affected typecheck/build/publish.
- `265` is repaired by this result; architecture review and the user functional trial remain pending.

STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
