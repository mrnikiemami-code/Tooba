# TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1 — Cart ARCH-COMPLETE-002 SoT Consistency Repair

Parent task: `TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001`
Track: `CART_ARCH_COMPLETE_002_SOT_REPAIR`
Backend-only: YES

Scope: repair **only** SoT consistency and durable guard coverage. No production business code,
validators, Cart foldering, Host production code, routes, DB/migrations, Order production code, or
frontend was modified.

## 1. Contradiction found by Architect

The parent task's implementation, guards and structure manifest were accepted, but
`docs/architecture/tmar-current-state.json` carried two contradictory Cart stories:

| Location | Was | Problem |
| --- | --- | --- |
| `structureLock.certifiedModules` | `["Order", "Cart"]` | Cart declared structure-certified |
| `hostCartBoundary.structureCertifiedUnderArchComplete002` | `false` | Same SoT claimed Cart was **not** certified |
| `completeReferenceModules.Cart.lastAcceptedTask` | `TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001` | Pointed at the prior Host-boundary task, not the certification task |
| `completeReferenceModules.Cart.lastAcceptedCommit` | `ec39c982...` (Host residual commit) | Same stale lineage |

So the SoT simultaneously asserted Cart was certified and not certified, and Cart's
complete-reference metadata still described the previous task. Cart had two different stories.

## 2. Corrected fields

`docs/architecture/tmar-current-state.json`:

- `structureLock.certifiedModules` = `["Order", "Cart"]` (unchanged; now coherent with the rest).
- `hostCartBoundary.structureCertifiedUnderArchComplete002` = `true`.
- All prior Host boundary facts preserved unchanged:
  `illegalCartAuthorityCount = 0`, `deadCartResidueCount = 0`,
  Cart-owned `ICartExpiryReconciler` expiry, Cart-owned `CartPersistenceHours` policy,
  `commerceHoldPolicyCartState`, removed Cart global usings, `MODULE_ENDPOINTS` route ownership,
  development-migration-only Host DB authority, `CONTRACTS_PORTS_ONLY` foreign boundary, `ICLOCK_IN_CART_BUSINESS_TIME`.
- `completeReferenceModules.Cart.lastAcceptedTask` = `TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001`.
- `completeReferenceModules.Cart.lastAcceptedCommit` = the actual implementation commit of that accepted
  task (`6e880942baaf3b2afd60fa0766e5e132704eca66`), following the existing SoT convention of pointing at the
  implementation commit rather than the later SoT stamp commit.
- No unrelated module metadata rewritten (Settlement/Fulfillment/Returns/Notification/Support/Wallet/Payment/Promotion/Offer/Order/Inventory untouched).
- Top-level current task metadata: `lastAcceptedTask = TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1`,
  `nextTask = USER_REVIEW_CART_ARCH_COMPLETE_002` (unchanged),
  `nextTaskGate = USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE` preserved,
  `checkoutState = PAUSED_AT_SAFE_W5_CHECKPOINT` preserved.

Commit-stamping convention: `lastAcceptedCommit` was first set to the sentinel `PENDING_COMMIT`,
the R1 commit was created, and the sentinel was then replaced with that real commit hash in the
SoT stamp commit — the same two-step convention used elsewhere in this SoT.

Recovery docs (`TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md`) record the R1 repair
lineage alongside the parent certification task.

## 3. Guard added / strengthened

`src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs` (`Recovery_current_state_is_fresh_and_machine_readable`):

- Every module listed in `structureLock.certifiedModules` must have a `structureCertified: true` entry in
  `tmar-module-structure-manifests.json`, and must not appear in `uncertifiedHttpOwningModules`.
- Cart is asserted present in `structureLock.certifiedModules`, present with `structureCertified=true` in
  the manifest, and absent from the uncertified list.
- `hostCartBoundary.structureCertifiedUnderArchComplete002` must be `true`.
- `completeReferenceModules.Cart.lastAcceptedTask` must start with the certification lineage
  `TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001`, with a non-empty commit.
- Order remains asserted certified.
- Top-level `lastAcceptedTask` updated to the R1 task id.

`TmarCompleteReferenceStructureGateTests` already asserted the same certified set is exactly `Order` + `Cart`
in both manifest and SoT, which composes with the strengthened durable guard.

The guard deliberately avoids hardcoding the R1/re-stamp commit hash; the invariant it protects is
task identity + certification-state consistency, not a specific hash value.

## 4. No production changes

Changed files are SoT/recovery documentation plus the durable guard test only. No production
business code, validators, foldering, routes, schema/migrations, Host production code, Order
production code, or frontend file was touched.

## 5. Validation

- `TmarDurableGuardTests`: PASS.
- `TmarCompleteReferenceStructureGateTests`: PASS.
- Combined run: 8 passed / 0 failed.
- `dotnet build src/backend/Tooba.slnx`: 0 errors.

## 6. Preserved state

- Cart: `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`, now consistently stated.
- Order: existing `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED` preserved.
- Host: `HOST_CART_ILLEGAL_AUTHORITY = 0`.
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`; W6 not started; frontend unchanged.
