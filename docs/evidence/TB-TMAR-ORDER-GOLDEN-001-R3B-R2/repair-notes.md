# TB-TMAR-ORDER-GOLDEN-001-R3B-R2 — typed contract faults

## Defect

R3B-R1 typed CQRS was accepted, but `AdminOrderOperationsOrchestrator` still
classified expected failures via `ex.Message` (`StartsWith` / `==` / `Contains` /
`MapKnownOperationException` / `TryMapReturnCode` / Persian prose).

## Repair

- Added `ContractOperationException` + `ContractOperationFault` in BuildingBlocks.
- Owning adapters promote stable-code `InvalidOperationException` → typed fault:
  - `FulfillmentAdminOperationsAdapter`
  - `ReturnAdminOperationsAdapter` (via `ReturnsExceptionMapper`)
  - `SettlementOrderAccrualAdapter` (via `SettlementExceptionMapper`)
  - `PaymentHostContractBridge`
  - `OrderInventoryLifecycleAdapter` / `CheckoutDirectory` restore path
- Orchestrator catches `AdminOrderOperationsException` / `ContractOperationException`
  and uses `.Code` only; unknown exceptions propagate.
- Deleted `MapKnownOperationException` / `TryMapReturnCode` from Application Ops.
- Architecture guard forbids `ex.Message` / message StartsWith / those helpers under
  `Admin/Operations/**`.
- Host FA tests assert `FulfillmentOpToFa(code)` only.

## SoT sync

- Order remains `INCOMPLETE_REFERENCE_REPAIR`
- `nextTask` = `TB-TMAR-ORDER-GOLDEN-001-R4` (not started)
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`
- Recorded R3B-R1 / R3B-R2 progression in `completedSlices` + notes
