# TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1 — RESULT

Status: **PASS within the issued A–E repair scope** (worker result — NOT Architect ACCEPT, NOT user functional
acceptance, NOT any provider/financial/UI claim).

Decision:

`EXPLICIT_INSPECTION_AT_EVERY_REACHABLE_ENTRY_FAIL_CLOSED_STORED_RECEIPT_EVIDENCE_WITH_ADDITIVE_STORE_CONSTRAINTS_AND_LOCAL_COMPOSITE_OWNERSHIP_A_TYPED_PENDING_STOCK_BINDING_BLOCKER_INSTEAD_OF_FALSE_COMPLETION_AND_A_SCOPED_SAVEPOINT_DECISION_COLLISION_RECOVERY_WITH_EXACT_PK_IDENTITY_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ONE_ADDITIVE_MIGRATION_APPLIED_AFTER_A_FRESH_VERIFIED_PRE_APPLY_BACKUP`

Parent 274 stays **PARTIAL** until the Architect reviews this repair; its own evidence and
`274/ARCHITECT-REVIEW.md` are unchanged and the original proof discrepancy is corrected only *here*.

## Environment / identity

| Item | Value |
| --- | --- |
| Expected-Base-SHA (envelope) | `145bb1a0747e6a97c607c06d22c374e34a001bc8` |
| Base-SHA (actual local `main` at claim) | `145bb1a0747e6a97c607c06d22c374e34a001bc8` (identical; == Architect docs-only review after parent code `7e7c2ee3d97506408112432775abeeb56715c3fd`) |
| Parent task code | `7e7c2ee3d97506408112432775abeeb56715c3fd` (TB-274 customer receipt foundation) |
| Code-SHA / Evidence-SHA / Final-HEAD / origin parity | recorded by the Bridge result after the normal commit + push (no self-referential hash pin inside these files) |
| Canonical runtime | `tooba_alpha` @ `127.0.0.1:5432`, edition `SingleStore`, tenant `store-alpha`, connection `tenant-alpha`; Host `5088`, FE `3000` |
| Frontend | **untouched** — no build, no test, no restart, no screenshot |

## A — inspection is explicit at every reachable entry

- `ReturnItemConditionParser.Parse` (`Application/Composition/ReturnItemConditionParser.cs` L33) refuses
  `null`/`""`/whitespace **and** any unknown token with the same typed `return.receipt.line_invalid`. There is no
  "default usable" path left; the previous `(also the default when omitted)` behaviour is gone.
- Defaults removed on all three reachable shapes: `AdminReturnReceiptLine` (Endpoints, wire),
  `CustomerReceiptLine` (Application typed command) and `CustomerReceiptLineCommand` (Contracts). A caller must state
  the condition deliberately; the numeric enum values and the `usable`/`damaged` wire tokens are unchanged.
- `ReturnAdminEndpoints.AdminRecordReceiptAsync` (L77–99) parses every line **before** `sender.Send`, so an omitted /
  null / blank / unknown token refuses with zero dispatch and zero case read.
- `ReturnAdminOperationsAdapter.ToDomainCondition` maps exactly `Usable`/`Damaged` and throws
  `return.receipt.line_invalid` for anything else — no "not damaged means usable" fallback for contracts callers.
- A receipt may never be inferred from a default `ReturnItem.Condition`; the stored inspection is always the
  submitted line's own condition.

## B — stored evidence and ownership fail closed

- `ReturnReceivingSummary.Describe` (`Domain/ValueObjects/ReturnReceivingSummary.cs`) now validates **every** stored
  fact before summing: a non-positive quantity, a non-representable (>6 dp) quantity, an undefined inspection
  (`(ReturnItemCondition)7`), a duplicate `(receipt, item, condition)` identity, an unknown/duplicate requested
  identity, an invalid requested quantity and a cumulative over-receipt all set `IsCoherent = false`. A contradictory
  row contributes **nothing** (`Received`/`Usable`/`Damaged` stay 0) — it is never subtracted, rounded or reclassified.
- New overload `Describe(requested, (ReturnReceiptId, …), owningReceiptIds)` binds each line to its **own** receipt
  header of this case: a line answering for another case's receipt is `IsCoherent = false` and never capacity.
- `ReturnDirectory.ReadReceivingAsync` (Receipts L215) is the ONE stored-fact read used by the receipt write and by
  every snapshot/action; `RecordReceiptAsync` re-reads it **after** the case `FOR UPDATE` and refuses a NEW occurrence
  on already-incoherent evidence with `return.recovery.evidence_conflict` and **0 new rows**.
- `ReturnDirectory.EnsureOwningSellerScope` (ReturnDirectory.cs L781) runs inside the owning directory **before** any
  decision/receipt/money write for `ApproveAsync` (L180) and `RejectAsync` (L335); a foreign seller scope answers
  exactly like an unknown case (`return.missing`) so nothing can be written across sellers and nothing leaks.
- `RecordReceiptAsync` refuses a command whose server-derived actor is empty (`return.receipt.actor_required`) before
  any case read; the actor always comes from the authenticated admin authority, never from the browser.
- Additive store constraints (new migration, see the migration proof): `quantity > 0`,
  `condition IN ('Usable','Damaged')`, local composite alternate keys and composite FKs tying a line's case id to
  **both** its receipt header's case and its requested item's case.

## C — never mark stock complete without owning Inventory confirmation

- `SettlePhysicalDispositionAsync` (`Directories/ReturnDirectory.CourierOrigin.cs`) takes the restock quantity from the
  immutable complete receiving summary (`Usable` of that line); the damaged portion is never sent to saleable stock.
- An inspected `Usable > 0` with no owning reservation (missing / null / `Guid.Empty`) is now a **typed pending
  blocker**: a warning is logged, `Disposition` stays `None`, **0 Inventory calls** happen and the confirmed money is
  untouched. The same fact is reported by the read policy as `return.recovery.stock_binding_required`
  (`Phase = Blocked`, no `RecoveryKind`, `FinancialComplete = true`, `PhysicalComplete = false`) and the
  `settle_physical_disposition` action refuses with exactly that code before any effect (`HasUnboundInspectedUsable`,
  `ReturnDirectory.Recovery.cs` L223).
- Settlement is persisted only after a real Inventory confirmation; the stable per-item key
  `return-restock-{case:N}-{item:N}` and the reload-on-save-failure semantics are retained.
- A **mixed** line publishes `SaleableRestockedQuantity` (the exact restocked usable portion) on both the Application
  and admin Contracts snapshots, so the requested quantity / the single condition field can never announce that the
  whole amount went back to saleable stock.
- A rejected/cancelled case that physically received goods with no disposition reports
  `return.recovery.physical_resolution_required` (no kind, no fake completion, money not re-sent, receipt history
  still visible). A still-`Requested` case awaiting the rest of its delivery is deliberately **not** reported as
  blocked — the owed-resolution blocker belongs to a case that was decided away.

## D — decision collision recovery matches its claimed proof

- `ReturnDirectory.Decisions.cs` now wraps the decision insert in `SAVEPOINT tb274_decision_intent` and releases it on
  success; on collision it runs `ROLLBACK TO SAVEPOINT` + `RELEASE` instead of touching the caller's transaction.
- **No global `ChangeTracker.Clear()`**: `DetachAttemptEntries(trackedBeforeAttempt)` detaches only entries added by
  this attempt (identity snapshot taken before `Add`), so a caller's tracked entities keep their instance, state and
  values.
- `IsDecisionKeyConflict` requires SQLSTATE `23505` **and** constraint `pk_return_request_decisions` **and** table
  `return_request_decisions` **and** schema `returns`; a lookalike constraint, or the same name on another
  table/schema, propagates unchanged and is never read as "another decision won".
- One winner comparison is used for the refusal: the stored winner of the same `Reject` kind with the same normalized
  reason replays; a different reason is `return.decision.reason_conflict` with **no mutation**; a legacy `null` reason
  is never overwritten; the scope refusal runs before the durable decision.

## E — backup / safe constraint apply / narrow proof

Ordered chronology (all timestamps local +03:30) is in `migration-and-backup-proof.md`: fresh `pg_dump` → readable
`pg_restore --list` verification → runner apply → post-apply plan (`pendingCount=0`). The **original** 274 archive was
verified *now* (not retrospectively) and labelled with the current verification time.

## Verification performed once after the final code change

| Suite | Result |
| --- | --- |
| `ReturnsCustomerReceipts274R1Tests` (new/affected, TB-274-R1) | **14/14** |
| `ReturnsCustomerReceipts274Tests` (retained parent gate suite) | **25/25** |
| `ReturnsCustomerReceipts274R1RelationalTests` (real `tooba_alpha`, rollback-only) | **4/4** |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** |
| Host publish (`Tooba.Host.csproj` → `artifacts/publish-primary`) | **0 errors** |

Declared deviations (disclosed, not papered over):

1. The worker additionally ran the **whole** `Tooba.Returns.Tests` project (105/105), the `ReturnsModuleAmsc001*`
   filter (34/34) and the retained `ReturnsCustomerReceipts274RelationalTests` + `ReturnsRecovery273RelationalTests`
   filter (9/9) **outside** the issued validation selection. All passed; none of them is claimed as module
   recertification, baseline improvement or independent Architect validation.
2. The R1 unit suite was executed more than once: the first passing run preceded two small corrections made during
   this task (a `Requested`-case precision narrowing in `ReturnRecoveryPolicy` and a mixed-line assertion). The
   reported 14/14 is the **final** run after the last code change.
3. The first migration apply attempt failed (`42703 column "ReturnRequestId" named in key does not exist`, 17:41:02)
   because the first generated key columns were unmapped PascalCase; EF's migration transaction rolled it back
   completely (the 17:44:10 pre-apply dump contains **none** of the new constraints, and the old single-column FKs are
   intact). The migration/snapshot/designer were corrected to the mapped snake_case columns, a **fresh** dump was taken
   and verified, and the runner applied it once at 17:44:21. No manual DDL, no partial schema, no data change.

## Live served state (Host-only refresh, backend only)

Host `5088` healthy on the freshly published assembly; `/health` 200, `/health/ready` 200
(`SingleStore`, messaging `Healthy`), `/__platform-commerce` → `store-alpha`. Read-only wire facts are listed in
`receipt-and-stock-integrity-proof.md` §5. FE was not rebuilt, restarted or inspected.

## Remaining limits (explicit)

- No live return case exists in the canonical database, so there is **no served operator walkthrough** of a real
  receipt/inspection; no demo case/order/hold/receipt was created and no live approval/reject/refund/receipt was
  executed.
- Money and stock are proven with controlled ports; the relational facts are **real PostgreSQL but rollback-only**
  (`committedRows = 0`).
- No committed two-writer race: the collision branch is proven with controlled exact error injection carrying the
  real constraint/table/schema identity, plus a rollback-only real savepoint/constraint fact on `tooba_alpha`.
- The cumulative upper bound (never over-receiving) stays in the serialized owning code by design; the store enforces
  positivity/defined-condition/ownership only.
- `SaleableRestockedQuantity` is additive and nullable; legacy rows keep `null` (no backfill).
- 275 (operator receiving/inspection UI + mobile) and 276 (connected acceptance) remain **NOT ISSUED**.

Worker PASS ≠ Architect ACCEPT. 272-R1, 273-R2 and shipping acceptances are preserved unchanged.
