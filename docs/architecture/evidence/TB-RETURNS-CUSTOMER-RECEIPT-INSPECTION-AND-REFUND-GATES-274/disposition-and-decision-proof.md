# TB-274 — physical disposition and persisted decision proof

Owning code: `ReturnDirectory.{CourierOrigin.cs,Decisions.cs,Recovery.cs}`, `ReturnDecisionRecord.cs`,
`ReturnAdminOperationsContracts.cs`, `ReturnsRequestValidators.cs`, `ReturnAdminEndpoints.cs`.
Focused suites: `ReturnsCustomerReceipts274Tests` (25/25), `ReturnsCustomerReceipts274RelationalTests` (6/6),
`ReturnsRecovery273Tests` (retained), `ReturnsModuleAmsc001*` (34/34).

## 1. Disposition is decided by the inspection, never by a default

`SettlePhysicalDispositionAsync` (shared, courier + ordinary) now derives an ordinary case's quantities from
`ReturnReceivingSummary` only:

- `usable = receiving.Lines[item].Usable` → one `RestockConsumedReservationAsync` with the owning per-item key
  `return-restock-{returnRequestId:N}-{returnItemId:N}`, then `SaleableRestock`;
- `damaged = receiving.Lines[item].Damaged` → `DamagedNonSaleable`, **zero** saleable Inventory calls;
- when no coherent+complete receipt exists nothing is restocked and the line stays `None`;
- the disposition is marked only after the owning Inventory call returns; a failed save reloads only that line so an
  unsaved disposition can never be read as durable;
- an already-disposed item is skipped, so a retried/uncertain money answer can never restock the same units twice.

Courier-origin keeps its original per-item identities and condition-derived quantities — unchanged.

## 2. Legacy disposition history is closed, not re-opened

`A_settled_physical_history_never_accepts_a_new_receipt` proves a case whose item already carries `SaleableRestock`
refuses a new receipt (`return.receipt.state_invalid`) and writes nothing, so a new receipt-line restock identity can never
double-restock a legacy case.

## 3. Persisted rejection reason

`returns.return_request_decisions.reason` is nullable `varchar(512)`; the column physically exists (verified read-only on
`tooba_alpha`) and is written by the existing winning-decision authority under the case-row lock.

| Behaviour | Proof |
| --- | --- |
| a new rejection without a meaningful reason writes nothing | `A_new_rejection_requires_a_meaningful_reason`; relational `C_a_rejection_without_a_meaningful_reason_writes_nothing` |
| the winning rejection stores the trimmed reason and replays exactly | `The_winning_rejection_persists_its_trimmed_reason_and_replays_exactly`; relational `C_the_winning_rejection_stores_its_trimmed_reason_and_a_changed_reason_is_a_typed_conflict` |
| a changed reason on the same winning rejection is `return.decision.reason_conflict` | `A_changed_reason_on_the_winning_rejection_is_a_typed_conflict` |
| an approval never records a rejection reason | `An_approval_never_records_a_rejection_reason` |
| the stored reason is exposed distinctly from the customer's request reason | `ReturnSnapshot.DecisionReason` vs `Reason` in `ReturnAdminOperationsContracts` / adapter |

The decision helper's unique-violation arbitration is narrowed to the real primary key `pk_return_request_decisions`, and
the former global `ChangeTracker.Clear()` plus broad catch are replaced by scoped failure handling that rethrows
provider/cancellation faults and preserves caller work.

## 4. Receipt history survives a rejection

`ReadReceivingAsync` is origin-based, not status-based, so a case that was rejected after goods were recorded keeps its
receipt/quantities visible in every snapshot; the rejection itself never restocks, releases or announces physical
completion, and no physical-resolution workflow was invented here (275 owns the operator surface).

## 5. What intentionally did not change

- no new numeric enum value, no shipped wire member removed, no new error catalog/route churn beyond the additive
  receipt route, snapshot fields and validator;
- no money amount, currency, destination, fee, tax or partial-refund algorithm;
- no courier-origin closure/claim/receipt change;
- no transaction or row lock is held across a gateway/Wallet/Inventory call (the receipt write is purely local and its
  own short transaction contains no external effect).
