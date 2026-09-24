# TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001 — Payment directory decomposition

## Parent acceptance

- Parent task: `TB-TMAR-PAYMENT-PRECERT-HYGIENE-001`
- Parent acceptance commit: `22849a17e31aa3357277c3979ac351a3f3f9f8e5` (Architect-ACCEPTED)
- This task: behavior-preserving decomposition only — Payment is still **NOT** ARCH-COMPLETE-002
  structure-certified.
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`.

## Before state

`Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs` was a 970/971 physical LOC, four-interface
god-file:

- declared `IPaymentDirectory, IPaymentReconciliationDirectory, IPaymentAdminDirectory, IPaymentExpiryDirectory`
- owned storefront initiation/verification **and** stale reconciliation **and** the admin
  operational/mutation/refund flow **and** unpaid expiry/reopen flow
- registered in `PaymentModule` with three cast-based down-casts:
  `(PaymentDirectory)sp.GetRequiredService<IPaymentDirectory>()`

## After state — four focused directories (one port each)

Namespace: `Tooba.Payment.Infrastructure.Directories`

| File | Class | Implements | Owned members |
| --- | --- | --- | --- |
| `PaymentDirectory.cs` | `PaymentDirectory` | `IPaymentDirectory` | `InitiateAsync`, `VerifyAsync`, `GetAsync`, `GetLatestForCheckoutAsync`, `HasSucceededPaymentForCheckoutAsync`, `RegisterProofAssetAsync`, `SubmitManualEvidenceAsync`, `RetryManualAfterRejectionAsync` |
| `PaymentReconciliationDirectory.cs` | `PaymentReconciliationDirectory` | `IPaymentReconciliationDirectory` | `ReconcileStalePendingAsync` |
| `PaymentAdminDirectory.cs` | `PaymentAdminDirectory` | `IPaymentAdminDirectory` | `GetOperationalAsync`, `GetLatestOperationalForCheckoutAsync`, `ReconcileAsync`, `ConfirmDepositAsync`, `RejectDepositAsync`, `RestoreDepositAsync`, `UnconfirmDepositAsync`, `CloseOrStartRefundForOrderCancelAsync`, `RestoreAfterOrderCancelRestoreAsync`, admin-only operational snapshot mapping |
| `PaymentExpiryDirectory.cs` | `PaymentExpiryDirectory` | `IPaymentExpiryDirectory` | `ExpireDueUnpaidAsync`, `ReopenExpiredForRetryAsync` |

No facade implements multiple ports. No compatibility wrapper. No type forwarding.

### Behavior-preserving details

- **Reconciliation** reuses the canonical path: stale Pending reads via `PaymentDbContext`
  (same `UpdatedAt <= cutoff`, `OrderBy(UpdatedAt)`, `Take(Math.Max(1, batchSize))`, latest-attempt
  lookup, processed-count) and then calls `IPaymentDirectory.VerifyAsync`. Verify is not duplicated.
- **Admin reconcile** reuses `IPaymentDirectory.VerifyAsync` (same `payment.missing` /
  `payment.attempt.missing` codes and latest-attempt selection).
- **Admin cancel/refund** keeps the typed fault contract: `ContractOperationException` with
  `ex.Code == "payment.refund.gateway.unconfigured"` preserves `RefundPending`; other `payment.*`
  codes call `MarkRefundFailed(ex.Code, clock.UtcNow)`. No `Message` classification. Unknown
  exceptions propagate.
- **Expiry** keeps the transaction boundary, `FOR UPDATE SKIP LOCKED`, status filters
  (`Created/Pending/Failed`), `unpaid_timeout_at` ordering, batch limit, returned `CheckoutId`s,
  actor access, gateway initiation, attempt creation, timeout assignment and clock/id behavior.
- `RetryManualAfterRejectionAsync` keeps its own actor check and delegates to
  `IPaymentAdminDirectory.RestoreDepositAsync` (no duplicated restore logic).
- `AlreadySucceeded()` helper and the payment/refund/admin codes are unchanged.

## Shared collaborators

Two narrowly named internal collaborators under
`Directories/Shared` (namespace `Tooba.Payment.Infrastructure.Directories.Shared`) remove the only
real duplication. No `Common`/`Helpers`/`Utils`/`Manager` dumping ground exists.

- `PaymentActorAccess.cs` — `EnsureActorCanSeeAsync` guard (`payment.access.order_identity_required`)
- `PaymentUnpaidTimeoutAssigner.cs` — `ICommerceHoldPolicy.ResolveUnpaidTimeoutAt` assignment

## Directory line counts (physical LOC)

| File | LOC |
| --- | --- |
| `PaymentDirectory.cs` | 476 (was 971; task limit <700) |
| `PaymentAdminDirectory.cs` | 378 |
| `PaymentExpiryDirectory.cs` | 136 |
| `PaymentReconciliationDirectory.cs` | 57 |
| `Shared/PaymentActorAccess.cs` | 25 |
| `Shared/PaymentUnpaidTimeoutAssigner.cs` | 20 |

No new Payment production file reaches the 800-LOC threshold.
`Baselines/tmar-source-size-baseline.json` was updated for the shrunk `PaymentDirectory.cs`
(removed from `OVERSIZED_LEGACY`; the file is now `NORMAL`).

## DI registration

`PaymentModule` registers each focused implementation directly:

```
IPaymentDirectory            -> PaymentDirectory
IPaymentReconciliationDirectory -> PaymentReconciliationDirectory
IPaymentAdminDirectory       -> PaymentAdminDirectory
IPaymentExpiryDirectory      -> PaymentExpiryDirectory
```

All `(PaymentDirectory)sp.GetRequiredService<IPaymentDirectory>()` down-casts are removed.
`PaymentDirectory` receives a deferred `Func<IPaymentAdminDirectory>` so the
`PaymentDirectory <-> PaymentAdminDirectory` construction cycle is broken without a service locator.
Behavior of every other registration is unchanged.

## Interface down-cast

`InterfaceDowncast = REMOVED` (guard asserts no `(PaymentDirectory)sp.GetRequiredService<...>`).

## Behavior parity / transaction semantics

`BehaviorParity = PRESERVED`. Callers consume the four ports; production construction sites changed
only in dependency wiring. The Host construction sites use the optional `Func<IPaymentAdminDirectory>`
default and remain compile- and behavior-clean. `TransactionSemanticsState = UNCHANGED`
(same `SaveChangesAsync` placement, DbContext ownership, transaction boundaries, outbox, idempotency,
status transitions and error codes).

## Schema / migration

`SchemaMigrationState = NONE`. No entity, column, model snapshot or migration change.

## PaymentContractBridge

`PaymentContractBridge` is intact and still resolves all three Contracts gateways
(`IPaymentAdminGateway`, `IPaymentCustomerGateway`, `IPaymentHoldSettingsGateway`), consuming the focused
admin/expiry/core directories.

## Message classification

`MessageClassificationState = REMOVED` — no `Message`-based classification was reintroduced.

## Payment -> Host / Host residue

- `PaymentToHostDependencyState = ZERO`
- Host residue remains exactly the two approved thin security adapters
  (`HostPaymentAdminAuthorizer.cs`, `HostPaymentStorefrontAuthorizer.cs`).

## Architecture guards

`PaymentArchitectureGuardTests` strengthened:

- `PaymentDirectory` implements only `IPaymentDirectory`; each focused implementation implements only
  its own port and no class implements more than one of the four ports.
- exact path↔namespace for the new files.
- no interface down-cast registration remains.
- `PaymentDirectory < 700` LOC; no Payment production file in `Directories` ≥ 800 LOC.
- no `Common`/`Helpers`/`Utils`/`Manager` folder; `Shared` holds exactly the two approved collaborators.
- Payment → Host ZERO; Host Payment residue stays two thin security adapters.
- no `Message` classification reintroduced.
- structure certification is **not** claimed.

## Focused validation

`Tooba.Payment.Tests`:

- Directory split: four distinct focused implementations and one-port-per-class assertions.
- Stale reconciliation delegates to canonical `Verify` (payment becomes `Failed` through the canonical path).
- Admin reconcile uses the canonical `Verify` path.
- Unpaid reopen creates a new attempt via the expiry directory.
- Cancel/refund typed fault tests (Unconfigured → `RefundPending`; other `payment.*` → `RefundFailed`;
  unknown exception propagates, untouched state).
- Wallet gateway typed rejection → `WALLET_SPEND_REJECTED`; unknown fault not swallowed.
- `PaymentContractBridge` preserves all three Contracts gateways.
- Admin grid missing enrichment returns empty display strings.

`Tooba.Payment.Tests`: **56/56 passed**.
`Tooba.Payment.Tests` architecture guards: green.

Host:
- `TmarDurableGuardTests`: green (new `paymentPrecertDirectorySplit` block asserted).
- `TmarCompleteReferenceStructureGateTests`: green (certified modules unchanged).
- `UnpaidOrderExpiryTests`, `StorefrontPaymentSucceededGuardTests`, `WalletCheckoutRefundTests`: green.

Pre-existing (not introduced by this task, present on the accepted parent commit):

- `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` — the
  pre-existing baseline drift (Host composers, CartDirectory, CatalogDirectory, FulfillmentDirectory,
  InventoryDirectory, Order orchestrator/CheckoutDirectory, CustomerPayment) remains. The
  `PaymentDirectory.cs` entry was **shrunk** (971 → removed from oversized baseline), not grown.
- `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count` and
  `...Infrastructure_to_foreign_Application_edges...` — pre-existing inventory/edge baseline drift.

These three failures reproduce identically on `22849a17` before this task's changes and are
out of scope for a behavior-preserving Payment directory split.

## Full build

`dotnet build src/backend/Tooba.slnx` — succeeded, 0 errors.

## Payment structure certification state

`PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`. Payment is not certified here.

## Other module certifications (preserved)

Cart, Order, StoreContext, Offer remain certified; Checkout W5 pause and `frontendFrozen = true` preserved.

## Recovery state

`docs/architecture/tmar-current-state.json`:

- `paymentDirectoryArchitecture = FOCUSED_DIRECTORIES_PAYMENT_RECONCILIATION_ADMIN_EXPIRY`
- `paymentDirectoryGodFile = REMOVED`
- `paymentHostResidue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY`
- `paymentPrecertHygiene = DEAD_PORTS_REMOVED_TYPED_FAULTS_NO_LOCALIZED_APPLICATION_FALLBACK`
- `structureCertification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`
- `lastAcceptedTask = TB-TMAR-PAYMENT-PRECERT-HYGIENE-001`, `lastAcceptedCommit = 22849a17…`
- `nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001`
- gate = `NEXT_TMAR_WAVE_AFTER_PAYMENT_PRECERT_DIRECTORY_SPLIT`

`TOOBA-TMAR-MASTER-RECOVERY.md` and `TOOBA-ARCHITECT-BOOTSTRAP.md` updated to match. Durable guard
expectations updated.

## Residual defects

- The three pre-existing `TmarSourceSizeAndInfraAppTests` baseline-drift failures listed above
  (out of scope; not introduced by this task).
- Payment is still pending ARCH-COMPLETE-002 structure certification (15 validators, exact
  path/namespace certification, manifest entry).
