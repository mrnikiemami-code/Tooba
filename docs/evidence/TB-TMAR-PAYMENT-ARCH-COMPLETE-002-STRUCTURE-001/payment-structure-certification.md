# TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001 — Payment structure certification

## 1. Scope

STRUCTURE / GUARD / MANIFEST / SoT closure only. No Payment behavior redesign,
no new validators, no directory refactor, no contract/event change, no
schema/migration change, no Host residue reopen, no Checkout resume, no frontend.

- Parent-Task: `TB-TMAR-PAYMENT-PRECERT-VALIDATION-002`
- Accepted parent commit: `659d4d06ab7bdce2a2c00b85d16bd308e1c0cf7d`
- Certification lock: `ARCH-COMPLETE-002`

## 2. Live physical folder set (authority)

Application: `Commands/` (11 request folders), `Queries/` (6 request folders),
`Errors/`, `Models/`, `Orchestration/`, `Ports/`, `Validators/{Admin,Storefront,Webhooks}`.

Endpoints: `Admin/`, `Errors/`, `Storefront/`, `Webhooks/` plus root
`PaymentEndpointModule.cs`.

Infrastructure: `Adapters/`, `DependencyInjection/`, `Directories/`,
`Directories/Shared/`, `Events/`, `Messaging/`, `Persistence/`,
`Persistence/Migrations/`, `Providers/`, `Workers/`.

No ceremonial folder was created; no file was moved.

## 3. Exact path ↔ namespace proof

`PaymentArchitectureGuardTests` now enforces **exact path-derived namespace
equality** for every Payment production `.cs` file (previous loose
`StartsWith`/prefix acceptance removed). Exemptions:

- EF migrations + model snapshot keep their legitimate migrations namespace.
- `GlobalUsings*.cs` declare no namespace by design (narrow alias guard below).

Acceptance is exact equality, so a namespace that merely starts with the
expected prefix fails. A live scan of all five production projects reported
zero mismatches; no mismatch repair was required.

## 4. Root allowlists (enforced)

| Project | rootAllowlist | forbiddenRootFiles (proof) |
| --- | --- | --- |
| `Tooba.Payment.Application` | `[]` | PaymentContracts, PaymentHandlers, PaymentRequests, PaymentQueries, PaymentQueryHandlers, StorefrontPaymentOrchestrator, PaymentErrorCodes |
| `Tooba.Payment.Endpoints` | `PaymentEndpointModule.cs` | PaymentStorefrontEndpoints, PaymentAdminEndpoints, PaymentWebhookEndpoints, IPaymentStorefrontAuthorizer, IPaymentAdminAuthorizer, IPaymentAdminGridQueryNormalizer, PaymentErrorCatalogContributor, PaymentErrorResources, PaymentEndpointLocalizer |
| `Tooba.Payment.Infrastructure` | `[]` | PaymentModule, PaymentDbContext, PaymentDirectory, PaymentAdminDirectory, PaymentReconciliationDirectory, PaymentExpiryDirectory, PaymentEvents, PaymentOutboxRegistration, PaymentReconciliationWorker, PaymentReconciliationOptions, PaymentGatewayRegistry |

Guard compares the actual top-level `*.cs` set to the allowlist and asserts every
forbidden root file is absent.

## 5. Alias-workaround result

`NO_NAMESPACE_ALIAS_WORKAROUND` enforced:

- No `TypeForwardedTo` / type-forwarding / compatibility shim in Payment production.
- No foreign-module global alias (`Tooba.{Order,Media,Inventory,Wallet,Cart,Offer}.*(Application|Infrastructure|Domain)`).
- Exactly one `GlobalUsings*.cs` file exists:
  `Tooba.Payment.Application/Orchestration/GlobalUsings.cs`, whose single
  `global using Tooba.Payment.Application.Orchestration;` is the self-namespace
  import. It hides no physical path mismatch and imports no foreign module layer;
  it is retained as a narrowly justified, explicitly recorded exception and is
  guarded so any additional/foreign global alias fails.

## 6. Validator inventory (certification prerequisite, unchanged)

`PaymentValidatorCoverageGuardTests` proves the complete endpoint inventory:

- endpoint-reachable requests = **16**
- validator-required = **15**, validators present = **15**
- no-validator-required = **1** (`ListStorefrontPaymentMethodsQuery`, NO_INPUT)
- worker-only = `ReconcileStalePaymentsCommand` = `NO_VALIDATOR_REQUIRED_INTERNAL_WORKER`, excluded from endpoint inventory
- all endpoint requests are `IRequest`, endpoints are `ISender`-only
- MediatR = `12.5.0`, no direct validator invocation

No business validation was merged into FluentValidation and no validator was added.

## 7. Directory decomposition preservation

- `PaymentDirectory` : `IPaymentDirectory` only, <700 physical LOC.
- `PaymentAdminDirectory` : `IPaymentAdminDirectory` only.
- `PaymentReconciliationDirectory` : `IPaymentReconciliationDirectory` only.
- `PaymentExpiryDirectory` : `IPaymentExpiryDirectory` only.
- No class implements more than one port; no cast-based registration.
- `Directories/Shared/` holds only the two approved narrow collaborators.

## 8. Host residue / cross-module

- Payment → Host dependency = **ZERO**.
- Host Payment residue = exactly `HostPaymentAdminAuthorizer` +
  `HostPaymentStorefrontAuthorizer` (thin security adapters).
- No reconciliation worker/options/grid policy returned to Host.
- No foreign DbContext; Payment.Application foreign dependencies remain Contracts-only.

## 9. Manifest change

`docs/architecture/tmar-module-structure-manifests.json`:

- Added `Payment` module: `structureCertified = true`, `lockVersion = ARCH-COMPLETE-002`
  with the three project entries from section 4.
- Payment removed from `uncertifiedHttpOwningModules` (now Settlement, Fulfillment,
  Returns, Notification, Support, Wallet, Promotion).

Certified module set:

`Order, Cart, StoreContext, Offer, Payment`

## 10. SoT closure

- `docs/architecture/tmar-current-state.json`
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
- `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md`
- `src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs`
- `src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs`

`nextTask = USER_REVIEW_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`,
`nextTaskGate = USER_REVIEW_REQUIRED_AFTER_PAYMENT_STRUCTURE_CERTIFICATION`.
No next module auto-selected.

## 11. Focused validation commands / results

- `PaymentArchitectureGuardTests` (focused filter) — PASS
- `PaymentValidatorCoverageGuardTests` (focused filter) — PASS
- `TmarCompleteReferenceStructureGateTests` (focused filter) — PASS
- `TmarDurableGuardTests` (focused filter) — PASS
- `dotnet build .../Tooba.Payment.Tests.csproj --no-restore` — 0 errors
- `dotnet build .../Tooba.Host.Tests.csproj --no-restore` — 0 errors

No broad suite, no solution build, no Testcontainers, no database tests.

## 12. Preservation

- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`
- `frontendFrozen = true`; no frontend production change
- No schema/migration change
