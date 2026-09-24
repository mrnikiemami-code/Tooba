# TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001 — Payment Host residue repair

Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Parent: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001 (Architect-ACCEPTED at `dcb8b416b19d8f9db90e8162754723e4cfbbfb14`)
Track: PAYMENT_HOST_RESIDUE_REPAIR
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE (frontendFrozen = true)

## 1. Audit acceptance

The parent Payment ARCH-COMPLETE-002 bounded audit is Architect-ACCEPTED. Verified closed decisions carried into this task:

- `HostPaymentAdminAuthorizer` = KEEP thin security adapter
- `HostPaymentStorefrontAuthorizer` = KEEP thin security adapter
- `HostPaymentAdminGridQueryNormalizer` = MOVE_TO_PAYMENT_MODULE
- `PaymentReconciliationHostOptions` = MOVE_TO_PAYMENT_MODULE
- `PaymentReconciliationHostedService` = MOVE_TO_PAYMENT_MODULE
- Payment structure certification remains a separate task
- 16 HTTP requests + 1 worker request audited; 15 HTTP validators deferred to the structure task
- `PaymentDirectory` / bridge naming / error debt deferred to the structure task
- zero Payment/Host production code changed by the audit

This task removes the two remaining Payment runtime ownership leaks from Host. It does **not** structure-certify Payment and does **not** add the 15 validators.

## 2. Five residue decisions (before → after)

| Host file | Audit decision | Action |
| --- | --- | --- |
| `Host/Tooba.Host/PaymentReconciliationHostedService.cs` | MOVE_TO_PAYMENT_MODULE | deleted |
| `Host/Tooba.Host/PaymentReconciliationHostOptions.cs` | MOVE_TO_PAYMENT_MODULE | deleted |
| `Host/Tooba.Host/Admin/HostPaymentAdminGridQueryNormalizer.cs` | MOVE_TO_PAYMENT_MODULE | deleted |
| `Host/Tooba.Host/Admin/HostPaymentAdminAuthorizer.cs` | KEEP thin security adapter | unchanged |
| `Host/Tooba.Host/Storefront/HostPaymentStorefrontAuthorizer.cs` | KEEP thin security adapter | unchanged |

## 3. Reconciliation worker/options (A/B)

Before: Host owned poll cadence, `PendingAgeMinutes`, `BatchSize`, command construction and reconciliation telemetry.

After — Payment-owned worker capability:

- `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Workers/PaymentReconciliationWorker.cs`
- `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Workers/PaymentReconciliationOptions.cs`

Namespaces match paths exactly (`Tooba.Payment.Infrastructure.Workers`). Options own the canonical section:

```
public const string SectionName = "Tooba:PaymentReconciliation";
```

Preserved defaults (unchanged from the current canonical configuration in `appsettings.json` / `appsettings.Production.json`):

| Setting | Default |
| --- | --- |
| `Enabled` | `true` |
| `PollIntervalSeconds` | `60` |
| `PendingAgeMinutes` | `5` |
| `BatchSize` | `20` |

Normalization is centralized on the options type at one Payment-owned boundary (no `Math.Max(...)` spread through the worker loop):

- `NormalizedPollInterval` — `PollIntervalSeconds < 5` → 5s
- `NormalizedPendingAge` — `PendingAgeMinutes < 1` → 1min
- `NormalizedBatchSize` — non-positive → 20

Preserved behavior: `BackgroundService` lifecycle, per-target iteration, generic tenant target source, per-target commerce-context assignment, scoped `ISender`, `ReconcileStalePaymentsCommand`, worker registry success/failure reporting, `PaymentGatewayInstrumentation` reconciliation telemetry, cancellation behavior and current logging intent.

## 4. Generic platform seams used (no Host concrete types)

The worker depends only on existing generic platform abstractions:

- `IOutboxPollTargetSource` (`Tooba.Persistence`)
- `IWorkerCommerceContextFactory` (`Tooba.Persistence`)
- `IBackgroundWorkerRegistry` (`Tooba.Persistence`)
- `ICommerceContextAssigner` (`Tooba.BuildingBlocks`)
- `IIdGenerator` (`Tooba.BuildingBlocks`)
- `ISender` (MediatR 12.5)

Host process adapters (`ConfiguredOutboxPollTargetSource`, `WorkerCommerceContextFactory`, `BackgroundWorkerRegistry`) remain in Host and are injected through the generic interfaces. `Payment.Infrastructure` has **ZERO** reference to `Tooba.Host`.

## 5. PaymentModule registration (C)

`src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs` now owns:

- `services.Configure<PaymentReconciliationOptions>(configuration.GetSection(PaymentReconciliationOptions.SectionName));`
- `services.AddHostedService<PaymentReconciliationWorker>();`

Safe registration cleanup while touching the file (no behavior change):

- removed duplicate `IPaymentGatewayCatalogPort -> PaymentGatewayCatalogAdapter`
- removed duplicate `IPaymentWebhookSignatureVerifier -> PaymentWebhookSignatureVerifierAdapter`
- fixed the malformed adjacent using line (`using Tooba.Payment.Contracts.Returns;using ...`)

## 6. Host Program cleanup (D)

`src/backend/Host/Tooba.Host/Program.cs` removed Payment-specific Host registration/configuration:

- `Configure<PaymentReconciliationHostOptions>(...)`
- `AddHostedService<PaymentReconciliationHostedService>()`
- `AddScoped<IPaymentAdminGridQueryNormalizer, HostPaymentAdminGridQueryNormalizer>()`

Kept generic platform worker services: `BackgroundWorkerRegistry`, `ConfiguredOutboxPollTargetSource`, `WorkerCommerceContextFactory` — plus the two approved Payment security adapters. Host no longer mentions `PaymentReconciliationHostOptions`, `PaymentReconciliationHostedService` or `HostPaymentAdminGridQueryNormalizer`.

## 7. Payment-owned admin grid normalization (E/F)

- Deleted `Host/Tooba.Host/Admin/HostPaymentAdminGridQueryNormalizer.cs`.
- Kept `Tooba.Payment.Endpoints/Admin/IPaymentAdminGridQueryNormalizer.cs`; XML doc updated (no longer says Host implements it).
- Added `Tooba.Payment.Endpoints/Admin/PaymentAdminGridQueryNormalizer.cs` (namespace `Tooba.Payment.Endpoints.Admin`) implementing the port.

Preserved exact field policy:

| Field | Kind | Searchable |
| --- | --- | --- |
| `reference` | Text | yes |
| `customer` | Text | yes |
| `amount` | Number | — |
| `status` | Enum | — |
| `supply` | Enum | — |
| `reservation` | Enum | — |
| `provider` | Text | yes |
| `created` | Date | — |
| `completed` | Date | — |

Preserved: default sort = `created`, default direction = `desc`, tie-break = `reference`, and existing paging/search/filter/operator normalization behavior.

Layering: the normalizer uses only `Tooba.BuildingBlocks.Grid` (`GridQueryPolicyBase`, `GridQueryOperators`, `GridQueryValidationException`) plus Payment-owned DTO/request shapes. It does **not** reference `Tooba.Host.Grid`, `AdminListGridPolicies`, `AdminReceiptListItem`, or any Host grid engine code.

Error mapping: field/operator violations now throw `SemanticException(new SemanticError("grid.filter.field.invalid" | "grid.filter.operator.invalid" | "grid.advancedFilter.field.invalid" | ...))`, which the central `ExceptionPresentationService` → `SafeErrorMapper` maps. The `grid.*` codes are stable and were already used by other module-owned grid policies; they are not registered as explicit catalog descriptors, so they fall to the safe generic 400 business fallback — the same behavior as the prior `PlatformHttpException` (400) path. Committed separately from the policies.

`PaymentEndpointModule.AddPaymentEndpointPresentation(...)` now registers:

```
services.AddSingleton<IPaymentAdminGridQueryNormalizer, PaymentAdminGridQueryNormalizer>();
```

Implementation is stateless, so `Singleton` is used. Host no longer registers this port.

## 8. Host Payment grid whitelist removal (G)

`src/backend/Host/Tooba.Host/Grid/AdminListGridPolicies.cs`: only the `Payments` policy block was deleted. Orders / Sellers / Content / ContentAuthors / Reviews / Stories are untouched.

`AdminReceiptListItem` in `Admin/AdminPanelModels.cs` existed only for the Payments policy; reference search proved zero remaining production uses after the removal, so it was removed. Orders/Sellers/Content/Reviews/Stories row models are unaffected.

## 9. Retained security adapters proof (H)

Only two Payment-named production Host files remain:

- `Host/Tooba.Host/Admin/HostPaymentAdminAuthorizer.cs`
- `Host/Tooba.Host/Storefront/HostPaymentStorefrontAuthorizer.cs`

Both are thin transport/security adapters only — no Payment business service, gateway choice, state transition, reconciliation rule, DB access, response composition, or grid field whitelist. Enforced by `PaymentArchitectureGuardTests.Host_owns_no_Payment_runtime_or_grid_residue` which asserts the exact remaining Payment-named Host file set.

## 10. No Payment → Host dependency

- `Tooba.Payment.Infrastructure.csproj` has no `Tooba.Host` reference (only `Payment.Application`, `Wallet.Contracts`, `Tooba.ModuleContracts`, `Tooba.Persistence`).
- The worker seam uses generic interfaces only; the guard asserts `ConfiguredOutboxPollTargetSource` / `WorkerStoreCommerceContextFactory` / `ControlPlaneRegistry` never appear in `PaymentReconciliationWorker.cs`.

## 11. Tests / guards (I)

Focused tests added:

- `src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentReconciliationWorkerTests.cs`
  - disabled worker exits without polling targets
  - options normalize unsafe values at one Payment-owned boundary
  - options preserve canonical defaults + section name
  - `PaymentModule` registers the worker/options and generic seams only

Strengthened:

- `PaymentArchitectureGuardTests.Payment_endpoints_cqrs_and_host_ownership_are_enforced` — worker is now asserted at the Payment path, uses generic seams, and has no Host concrete types.
- `PaymentArchitectureGuardTests.Host_owns_no_Payment_runtime_or_grid_residue` (new) — Host has no reconciliation worker/options, no grid normalizer, no Payments grid policy, and only the two approved thin security adapters.
- `PaymentArchitectureGuardTests.Payment_owns_admin_grid_normalizer_without_host_grid_engine` (new) — module-owned normalizer has no Host grid dependency and is registered by the endpoint module.
- `AdminListGridQueryEngineTests` — Host `AdminListGridPolicies` has no `Payments` policy; Payment-owned normalizer rejects an unknown field and preserves default sort + paging.
- `HostFolderStructureTests` root allowlist updated (Payment root files removed).
- `HostCartResidualGuardTests` Cart-naming allowlist updated (Payment worker shell entries removed).

No structure certification assertions were added.

## 12. Exact validation

- `dotnet test` — `Tooba.Payment.Tests`: **Passed 22 / Failed 0**
- `dotnet test` — Host focused guards (`HostFolderStructureTests`, `AdminListGridQueryEngineTests`, `AdminDbNativeGridQueryTests`, `HostModuleEndpointOwnershipTests`, `HostCartResidualGuardTests`): **Passed 27 / Failed 0**
- `dotnet test` — Host TMAR + Payment (`TmarDurableGuardTests`, `TmarCompleteReferenceStructureGateTests`, `~Payment`): **Passed 85 / Failed 0 / Skipped 2**
- `dotnet build src/backend/Tooba.slnx`: **0 Error(s)**

`TmarSourceSizeAndInfraAppTests` (3 failures) are **pre-existing at pristine HEAD** and unrelated to this task: every file named in those violations (`AccessControlDevelopmentSeed.cs`, `CatalogDirectory.cs`, `CartDirectory.cs`, `StorefrontComposer.cs`, `Order.Infrastructure → foreign Application` edges, and the source-size inventory count mismatch) is untouched by this diff. They were also observed at pristine HEAD during the sibling Offer tasks.

## 13. Behavior parity

Runtime reconciliation behavior and admin payments grid behavior are preserved:

- same worker name (`payment-reconciliation`), same loop/delay/cancellation, same per-tenant scope + `ISender` dispatch of `ReconcileStalePaymentsCommand`, same telemetry and registry reporting;
- same effective defaults from the same canonical section `Tooba:PaymentReconciliation`;
- same grid whitelist, default sort/direction, tie-break and normalization semantics.

## 14. Recovery state

- `lastAcceptedTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001`
- `lastAcceptedCommit = dcb8b416b19d8f9db90e8162754723e4cfbbfb14`
- `paymentHostResidue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY`
- `reconciliationOwnership = PAYMENT_INFRASTRUCTURE_WORKER_AND_OPTIONS`
- `adminGridPolicyOwnership = PAYMENT_ENDPOINTS`
- `hostPaymentSecurityAdapters = HostPaymentAdminAuthorizer,HostPaymentStorefrontAuthorizer`
- `structureCertification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`
- `nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001`

Preserved: Cart / Order / StoreContext / Offer certifications; Checkout `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`. Payment is **not** structure-certified by this task.
