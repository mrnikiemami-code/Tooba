# TB-265-R2 worker result — exact release identity, serialized decision, partial source replacement conservation, operator handoff

Status **PASS** within the issued 265-R2 repair scope, ready for Architect review and the user's own trial.
Decision **EXACT_IMMUTABLE_RELEASE_REQUEST_IDENTITY_WITH_SERIALIZED_DURABLE_APPROVE_VS_REJECT_DECISION_AND_PARTIAL_SOURCE_REPLACEMENT_RETURN_THAT_CONSERVES_THE_REMAINING_ORDERS_OWN_OBLIGATION**.
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "shipping complete", NOT a live payment/provider/wallet refund.

Base `a0e1f24e55f14f8bc366c9018da9f117e718aca8` (verified ancestor). Single worker, one task, no delegation, no historical agent
resumed. Code/evidence/HEAD SHAs and origin parity are in `result-contract.txt`.

## 1 — Inventory: the recorded row now answers only its OWN request, truthfully

`InventoryReturnObligationRelease` persists a new additive owning fact `HeldObligationReleaseRecord`
(`inventory.held_obligation_releases`, migration `inventory 20261114150000_AddHeldObligationReleases`) holding the immutable
request — `Purpose`, `IdempotencyKey`, `ReservationId`, `RequestedQuantity` — next to the actual effect
(`ReleasedQuantity`, `Outcome`, `ProcessedAt`). The three former key-only branches (initial read, inside-lock read, and the
concurrency winner after a unique-key violation) all pass through the SAME `AnswerRecorded`, which compares purpose,
reservation and requested quantity and throws `inventory.held_release.intent_conflict` (zero writes) on any difference. The
answer is truthful: a `Consumed` original sale is reported `Consumed` with `released=0`, a fully-released reservation
`AlreadyReleased`, and a partially released one `PartiallyHeld` — never "Released" for a no-op. The transaction still spans
the reservation row lock (`SELECT … FOR UPDATE`), the `Reserved` decrement (`OnHand` untouched) and the durable row.

Proven on real `tooba_alpha` — **L**: requested 2 of a 3 hold → `released=2 outcome=PartiallyHeld`, exact replay answers
**2** (not the remainder 1) with `Replayed`, a changed requested quantity and a changed reservation both answer
`held_release.intent_conflict` with `holdLeft=1 reserved=1 identities=1 otherTouched=0`; **M**: an abandoned commit leaves
`identities=0` and the exact retry performs the effect once (`retryReleased=1 finalHold=2 finalReserved=2`).

## 2 — Returns: the approve-vs-reject decision is durable and serialized; completed replay answers its completion

New additive fact `ReturnDecisionRecord` (`returns.return_request_decisions`, PK on the case, migration
`returns 20261114170000_AddReturnRequestDecisions`). `ReturnDirectory.Decisions.cs` writes it under
`SELECT 1 FROM returns.return_requests … FOR UPDATE` **before** any money/stock/claim effect, in a short transaction that
never spans an external payment call; the primary key is the durable guard when a racing context did not wait on the lock.
Both public paths share that one authority: a recorded `Reject` makes approval answer `return.already_rejected`, a recorded
`Approve` makes rejection answer `return.already_approved`. Two approvals share the SAME deterministic financial attempt key
(`refund-{caseId:N}`), so the second writer loses on the unique key and RESUMES the recorded attempt instead of minting a
second one. A courier case that is genuinely `Completed` at `RefundReleased` answers its recorded completion before the
ordinary transition guard (no new attempt/money/disposition).

Proven on real `tooba_alpha` — **N** (deterministic barrier, independent contexts): `winner=Rejected loser=return.already_rejected
decisions=1 refundCalls=0`; **O** completed replay: `attempts=1 refunds=1 replayRefunds=0 replayAttempts=1`; **Q** two
concurrent approvals: `distinctKeys=1 attempts=1 decisions=1 status=Completed phase=RefundReleased`.

**Defect found and fixed during the live handoff check (this round):** on a payment-less fixture, approving a case whose
durable decision was already `Reject` reached the payment resolver first and answered `refund.payment_missing` (404) instead
of the case's own decision. `ApproveAsync` now reads the recorded decision (read-only, no row invented) before any payment
lookup, so a payment-less case stays rejectable and the opposite decision is answered as `return.already_rejected`.
Observed live before/after on the same case: `404 refund.payment_missing` → `409 return.already_rejected` with the Persian
title «این مرجوعی قبلاً رد شده است.» and zero writes.

## 3 — Fulfillment: a partial source replacement return conserves what the order still owns

`DamagedGoodsResolution` gained `CaseSettledQuantity`/`RemainingQuantity` and `SettlePartialCase(quantity)` (additive
`case_settled_quantity` column, migration `fulfillment 20261114160000_AddPartialCourierReturnSettlement`).
`CourierReturnSourceDirectory` now plans and PERSISTS the executable allocation (`EnsureAllocationsAsync`, per-line own hold,
own cancelled parcel) **before** the Order closure runs, so an accepted claim can never become an unsupported partial branch
after the obligation was closed. `Plan` no longer throws `replacement_partial_claim_unsupported` when the existing
resolution is larger than the selected quantity: it settles exactly the selected portion (`caseSettled=1 of 3`), cancels and
rebinds only the selected allocation, and re-prepares the unselected remainder as its own parcel on the order. The
`ActiveKey` is per planned allocation (source receipt + line + condition + allocatable remainder), so a disjoint partial
claim is not falsely blocked by an already-claimed sibling portion — the common serialized capacity authority decides.
`ExecuteSerializedAsync` retries once on PostgreSQL `40001` so two racing contexts observe the committed capacity.

Proven on real `tooba_alpha` — **P**: receipt damaged 3, created replacement/hold 3, case selects 1 →
`selected=1 decided=3 caseSettled=1 remaining=2 holdLeft=2 status=Held parcel=Cancelled releasedHeld=1 refunds=1 restocks=0
onHand=7 repreparedParcel=… quantity 2 replayRefunds=1 siblingB=0`; **B/C/H/I** retain the source-exact claim behaviour.
Live read of the same shape after reload: returned parcel 3 → selected unit case-bound and cancelled → new `Created` parcel
with quantity **2**, sibling line untouched.

## 4 — Operator handoff (real public command, persisted, reloaded)

One positive task-owned case through the real HTTP command (no fixture manufacturing by the user, no UI automation):

- `POST /v1/admin/orders/0dabee2e-c347-408e-a058-59876121a472/operations` `create_courier_return` → 200, case
  `01a1d8a1-b65e-7000-b5f9-c65e06529cfa` `Requested`, `origin=CourierOrigin`, claim
  `01a1d8a1-b621-7000-b00e-5a553c148509`, `closurePhase=None`, `releasedHeldQuantity=0`, `refundAttempts=[]`.
- The same reviewed key replayed → the SAME case/claim (claims for that parcel stay **1**); no second case, no second claim.
- **Reload after republishing/restarting the Host** → the order read model still returns
  `courierReturnCases:[{claimId, returnRequestId, status:"Requested"}]` (the non-empty list is what renders the single 44px
  «مشاهدهٔ پروندهٔ مرجوعی»), and `GET /v1/admin/returns/{case}` returns the courier origin, source parcel/receipt/claim and
  the Damaged/Usable condition.
- Reject path live: `reject_return` wrote exactly one durable decision (`Reject`, the dev actor), released the claim
  (`Rejected`) and touched **no** reservation (0 rows) and no money (`refundAttempts=[]`).
- The approval side is exercised through the owning application with **controlled financial ports** (task-owned
  `IPaymentRefundGateway` / `IPaymentReturnReader` / `IWalletRefundCreditPort`), never a real provider, wallet or card. No
  live approval/refund was executed on the canonical runtime, because that would move real money; the boundary is disclosed
  rather than simulated in the UI.

Full commands and responses: `tb265r2-live-operator.txt`. Screens were not captured as images; the touched surfaces are the
already-shipped ones (no FE source change was needed this round beyond localization), and the served production bundle was
verified to carry the copy.

## Verification actually run (once each after the final change)

| Check | Result | Log |
| --- | --- | --- |
| Fulfillment `CourierReturnClaim265Tests` | **8/8** | `tb265r2-fulfillment.txt` |
| Inventory `ReturnObligationRelease265Tests` | **5/5** | `tb265r2-inventory.txt` |
| Returns `CourierOriginReturns265Tests` | **5/5** | `tb265r2-returns.txt` |
| Host `CourierOriginReturns265RelationalTests` on real `tooba_alpha` (A–K retained + L,M,N,O,P,Q) | **17/17** | `tb265r2-relational-log.txt`, facts in `tb265-relational.txt` |
| Order `AdminOrderPostDispatchReplayTests` (public command boundary) | **16/16** | `tb265r2-order-replay.txt` |
| FE `npm run test:returns` | **7/7** | run in `src/frontend` |
| FE `app/admin/admin-error-map.test.ts` | **7/7** | run directly |
| FE `tsc --noEmit` | **0** | |
| FE `npm run build` | **0**, BUILD_ID `PtxAhyJO9UhnwD8B3_ln4`, chunk `1323-a6cb5f1ef95dfaee.js` carries `held_release` | |
| Solution `dotnet build Tooba.slnx -c Release` | **0 errors** (213 warnings) | `tb265r2-build.txt` |
| Host `dotnet publish -o artifacts/publish-primary` | **0 errors** | `tb265r2-publish.txt` |
| Migration plan `--tenant store-alpha` | Inventory `20261114150000_AddHeldObligationReleases`, Fulfillment `20261114160000_AddPartialCourierReturnSettlement`, Returns `20261114170000_AddReturnRequestDecisions`; every module `pendingCount=0` | `tb265r2-plan.txt` |

## Deploy / environment

ONE canonical `tooba_alpha` @127.0.0.1:5432 (SingleStore, tenant `store-alpha`), Host 5088 (PID **10004**, `/health` 200,
`/health/ready` 200 → SingleStore), Next 3000 (PID **17188**, `/fa` 200, BUILD_ID `PtxAhyJO9UhnwD8B3_ln4`). The three
additive migrations were applied only through `Tooba.MigrationRunner` after the private backup
`D:\temp\tb265r2-backup\tooba_alpha-pre-tb265-r2.dump` (3,570,765 bytes). No extra/disposable/Docker/15433 database, no
edition/connection/port/appsettings switch, no manual DDL, no raw business SQL, no reset/reseed/delete/backfill, no global
reclaimer run. DataProtection and CapturedPreview untouched (local ON 7 dates / checked-in OFF). User rows, the shipping
draft `01a17d0d-…` and the screenshot fixture stay read-only. All new rows are task-labelled `TB265-…` / `tb265r2-…`. 255
parked, live Tipax deferred, parent common-package return still unsupported with the real child guards.

## Localization added this round

`admin-error-map.ts` gained `inventory.held_release.intent_conflict`, `inventory.reservation.release_mismatch` and
`inventory.return_restock.invalid` (Persian + English, no machine code reaches the operator), and the now-impossible
`fulfillment.returns.replacement_partial_claim_unsupported` copy was removed because a partial source replacement claim is
supported. `admin-error-map.test.ts` asserts all four facts.

## Honest limits

- Approval/refund is proven through the owning application with controlled financial ports; **no** real payment, provider,
  wallet credit or refund moved, and the live review case is deliberately left `Requested` for the user.
- No worker browser click-through of the dialog; the operator side is proven by the real public command, the real read
  models after restart, the served production bundle and the owning relational cases.
- Concurrency is proven with two independent real contexts plus DB guards, not a stress test.
- A returned parcel whose capacity is entirely consumed truthfully offers no action; the parent consolidated-package return
  workflow remains unsupported.
- This is a worker PASS pending Architect review; it is not user acceptance and not "all shipping fixed".

---

## AUDIT CORRECTION appended by TB-266 (2026-11-14)

The fixture-construction statement in this result was too strong. The relational cases in this folder built a Paid seller order by direct SQL, inserted a checkout without an owning payment confirmation, and constructed FulfillmentUnit.CreateFromPaidOrder(...) in-process. They prove the aggregate, the claim and the closure policy — **not** that an unpaid checkout can reach dispatch, and **not** that the paid state came from the owning payment path. The durable closure/claim/decision invariants themselves are unaffected.

The full correction and the TB-266 counter-evidence (live unpaid order with no fulfillment, plus the ulfillment.order.payment_not_authorized guard) are in
\docs/architecture/evidence/TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266/audit-correction-265.md\.
