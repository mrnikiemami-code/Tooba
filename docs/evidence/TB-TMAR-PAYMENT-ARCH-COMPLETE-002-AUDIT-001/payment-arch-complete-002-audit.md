# TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001 — Payment Host residue + ARCH-COMPLETE-002 bounded audit

Task: `TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001`
Parent: `TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001` (ARCHITECT-ACCEPTED at `8d7f5e531ac50b263ed79a5c511802e3e1a013bb`)
Channel: `tooba-main` · Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE` · Track: `PAYMENT_ARCH_COMPLETE_002_BOUNDED_AUDIT`
Audit only: **zero Payment/Host production code changed in this task.**

## 0. Parent recovery stamp

- `lastAcceptedTask = TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001`
- `lastAcceptedCommit = 8d7f5e531ac50b263ed79a5c511802e3e1a013bb`
- Offer = `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`
- `nextTask = USER_REVIEW_PAYMENT_ARCH_COMPLETE_002_AUDIT_001`
- Preserved: `Cart/Order/StoreContext/Offer` certified set, `Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT`, `frontendFrozen = true`

## 1. Payment current state (verified on disk)

| Attribute | Value |
| --- | --- |
| `state` | `COMPLETE_REFERENCE_PATTERN` |
| `httpApplicability` | `HTTP_OWNING` |
| `endpointOwnership` | `MODULE_ENDPOINTS` |
| `cqrs` | `MEDIATR_12_5` (MediatR 12.5.0 in `Tooba.BuildingBlocks.csproj`) |
| `lastAcceptedPaymentTask` | `TB-TMAR-PAYMENT-GOLDEN-001-R2` |
| `ARCH-COMPLETE-002 structureCertified` | **false** (absent from `modules[]`, still listed in `uncertifiedHttpOwningModules`) |

Payment production projects: `Tooba.Payment.Domain`, `.Contracts`, `.Application`, `.Infrastructure`, `.Endpoints`.

## 2. A. Host Payment residue classification

Five Host Payment-named files exist (`Program.cs` call sites at lines 103, 142, 203, 204, 205).

| Host file | Classification | Proof / rationale |
| --- | --- | --- |
| `Admin/HostPaymentAdminAuthorizer.cs` | `KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER` | Body is a single `AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment)` call after resolving `CurrentAuthenticatedSession`/`ICurrentTenant`/`IAuthorizationGuard`/`IHostEnvironment`. No Payment business decision, no payment-state transition, no gateway choice, no persistence, no response composition. Correct thin security boundary. |
| `Storefront/HostPaymentStorefrontAuthorizer.cs` | `KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER` | Resolves `CurrentAuthenticatedSession` and returns `session.UserId` or `null`. Pure session→actor-id adapter (mirrors `HostOrderStorefrontActor` precedent). No Payment policy. |
| `Admin/HostPaymentAdminGridQueryNormalizer.cs` | `MOVE_TO_PAYMENT_MODULE` | Delegates to `AdminListGridPolicies.Payments`, a **Host-owned** `AdminListGridQueryPolicy<AdminReceiptListItem>` whose whitelist is Payment-specific: `reference, customer, amount, status, supply, reservation, provider, created, completed`. Its row shape `AdminReceiptListItem` is Host-owned and itself maps off Payment's `AdminPaymentGridItemDto`. Every other certified module that moved its queue grid to module ownership had the Host `AdminListGridPolicies` entry **deleted** — Settlement (`Assert.DoesNotContain("Payouts", hostGrid)`), Returns (`Assert.DoesNotContain("AdminListGridPolicies.Returns", hostGrid)`), Fulfillment (`Assert.DoesNotContain("Fulfillments", hostGrid)`). Payment is the remaining outlier. Payment already owns `IPaymentAdminGridQueryNormalizer` in `Endpoints/Admin` and already owns `QueryAdminPaymentsGridQuery` + `AdminPaymentGridQueryInput` in Application. The grid field whitelist for the payments list is **Payment's** policy, so it must be Payment-owned. |
| `PaymentReconciliationHostOptions.cs` | `MOVE_TO_PAYMENT_MODULE` | Holds Payment-specific scheduling policy: `Enabled`, `PollIntervalSeconds` (60), `PendingAgeMinutes` (5), `BatchSize` (20). The Reconciliation precedent is explicit: `ReservationCycleOptions` lives in `Tooba.Order.Application.ReservationCycle.Contracts` and is configured by `OrderModule` (`services.Configure<ReservationCycleOptions>(...)`); the Host worker reads it via contracts. |
| `PaymentReconciliationHostedService.cs` | `MOVE_TO_PAYMENT_MODULE` | The Host shell currently owns Payment-specific **command construction** (`new ReconcileStalePaymentsCommand(TimeSpan.FromMinutes(Math.Max(1, _options.PendingAgeMinutes)), _options.BatchSize)`), **business timing** (the `TimeSpan.FromMinutes(PendingAgeMinutes)` pending-age threshold and the `BatchSize` batch policy), **failure semantics** (throws when `result.IsFailure`), the loop/poll loop, and **telemetry** (`PaymentGatewayInstrumentation.RecordReconcile`) — a Payment.Infrastructure type. The `ISender`-dispatch-only shape is correct, but the correct boundary is worker-in-module: `CartExpiryWorker` already lives in `Tooba.Cart.Infrastructure/Lifetime/CartExpiryWorker.cs` and is registered by `CartModule` (`services.AddHostedService<CartExpiryWorker>()`). Payment must follow the same shape. |

**No `REMOVE_DEAD_RESIDUE`** among the five: all five are registered in `Program.cs` and reached at runtime.

### Reconciliation worker ownership decision

**DECISION — `PAYMENT_INFRASTRUCTURE_OWNS_WORKER_AND_OPTIONS`.**

- `PaymentReconciliationHostedService` → `Tooba.Payment.Infrastructure/…` (rename to a Payment-owned worker name, e.g. `PaymentReconciliationWorker`) and `AddHostedService<…>` moves into `PaymentModule.AddServices(...)`.
- `PaymentReconciliationHostOptions` → Payment-owned options type (e.g. `Tooba.Payment.Infrastructure` or `Payment.Application` contracts) with `SectionName` `Tooba:PaymentReconciliation`; `PaymentModule` owns `services.Configure<...>(configuration.GetSection(SectionName))`.
- The worker keeps using only the generic platform seams already in `Tooba.Persistence/WorkerSeams.cs` (`IOutboxPollTargetSource`, `IWorkerCommerceContextFactory`, `IBackgroundWorkerRegistry`) — they are **generic platform adapters**, not Payment business. The Host process-level implementations (`ConfiguredOutboxPollTargetSource`, `WorkerCommerceContextFactory`, `BackgroundWorkerRegistry`) stay in Host and are injected by interface. **No module→Host reference is introduced.**
- Payment-owned options must clamp/validate at the options boundary (`PollIntervalSeconds > 0`, `PendingAgeMinutes >= 1`, `BatchSize > 0`) so `ReconcileStalePaymentsCommand` needs no transport validator.
- Host `Program.cs` drops lines 103 and 142 and keeps generic `Tooba:PaymentReconciliation` config binding inside the module read from `IConfiguration`.

### Grid-normalizer ownership decision

**DECISION — `PAYMENT_OWNS_GRID_WHITELIST; KEEP_A_THIN_HOST_SECURITY_ADAPTER_ONLY`.**

- Payment owns the whitelist/normalize policy for the admin payments list (Payment-owned policy over `AdminPaymentGridItemDto`, plus a Payment-owned default normalizer implementation registered by `PaymentModule`).
- Host `AdminListGridPolicies.Payments` and the Host `AdminReceiptListItem` coupling for payments are removed.
- `HostPaymentAdminGridQueryNormalizer.cs` is deleted.
- The only Host item retained in the chain is `HostPaymentAdminAuthorizer` (panel gate), which is already classified `KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER`.
- The optional Host grid **primitive** (`AdminListGridQueryPolicy<T>` / `BoundedListGridQueryEngine` / `InMemoryGridField<T>`) may be reused only as a generic primitive if Payment is already permitted to consume `Tooba.BuildingBlocks.Grid` (it is — `GridQueryRequest` already crosses `IPaymentAdminGridQueryNormalizer`); the smallest allowed adapter is the existing generic policy type, with **no Payment field list left in Host**.
- Guard consequence: Payment's guard must assert `AdminListGridPolicies` contains no `Payments`/`AdminReceiptListItem` entry, mirroring Settlement/Returns/Fulfillment.

## 3. B. Endpoint + MediatR inventory (exhaustive)

`Tooba.Payment.Endpoints` request constructions (exactly **16**, all `ISender`-dispatched, all with real MediatR handlers):

| # | Request | Kind | Endpoint | Handler | `IRequest` | `ISender` | Direct Directory/Application call |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `InitiateStorefrontPaymentCommand` | Command | Storefront `POST /checkout/{checkoutId}/payments` | `Tooba.Payment.Application/Commands/InitiateStorefrontPayment` | yes | yes | none |
| 2 | `GetStorefrontWalletQuoteQuery` | Query | Storefront `GET /checkout/{checkoutId}/wallet-quote` | `Queries/GetStorefrontWalletQuote` | yes | yes | none |
| 3 | `ListStorefrontPaymentMethodsQuery` | Query | Storefront `GET /payment-methods` | `Queries/ListStorefrontPaymentMethods` | yes | yes | none |
| 4 | `GetStorefrontPaymentQuery` | Query | Storefront `GET /payments/{paymentId}` | `Queries/GetStorefrontPayment` | yes | yes | none |
| 5 | `GetStorefrontPaymentSandboxContextQuery` | Query | Storefront `GET /payments/{paymentId}/sandbox` | `Queries/GetStorefrontPaymentSandboxContext` | yes | yes | none |
| 6 | `CompleteSandboxPaymentCommand` | Command | Storefront `POST /payments/{paymentId}/sandbox/complete` | `Commands/CompleteSandboxPayment` | yes | yes | none |
| 7 | `SubmitManualPaymentEvidenceCommand` | Command | Storefront `POST /payments/{paymentId}/manual-evidence` | `Commands/SubmitManualPaymentEvidence` | yes | yes | none |
| 8 | `RetryManualPaymentCommand` | Command | Storefront `POST /payments/{paymentId}/manual-retry` | `Commands/RetryManualPayment` | yes | yes | none |
| 9 | `RetryUnpaidPaymentCommand` | Command | Storefront `POST /payments/{paymentId}/unpaid-retry` | `Commands/RetryUnpaidPayment` | yes | yes | none |
| 10 | `UploadManualPaymentProofCommand` | Command | Storefront `POST /payments/{paymentId}/proof` | `Commands/UploadManualPaymentProof` | yes | yes | none |
| 11 | `ProcessPaymentWebhookCommand` | Command | Webhooks `POST /v1/payments/webhooks/{providerCode}` | `Commands/ProcessPaymentWebhook` | yes | yes | none |
| 12 | `GetAdminPaymentQuery` | Query | Admin `GET /payments/{paymentId}` | `Queries/GetAdminPayment` | yes | yes | none |
| 13 | `ReconcileAdminPaymentCommand` | Command | Admin `POST /payments/{paymentId}/reconcile` | `Commands/ReconcileAdminPayment` | yes | yes | none |
| 14 | `ConfirmAdminDepositCommand` | Command | Admin `POST /payments/{paymentId}/confirm-deposit` | `Commands/ConfirmAdminDeposit` | yes | yes | none |
| 15 | `RejectAdminDepositCommand` | Command | Admin `POST /payments/{paymentId}/reject-deposit` | `Commands/RejectAdminDeposit` | yes | yes | none |
| 16 | `QueryAdminPaymentsGridQuery` | Query | Admin `POST /payments/query` | `Queries/QueryAdminPaymentsGrid` | yes | yes | none |

`Tooba.Payment.Endpoints` contains no `IPaymentDirectory` / `IPaymentAdminDirectory` / `DbContext` / `catch` / `ex.Message` usage; total IRequest-bearing Application types = 17, so the endpoint set above is complete and nothing is left unclassified.

### Worker-only request

| Request | Reachability | Handler | Ownership path |
| --- | --- | --- | --- |
| `ReconcileStalePaymentsCommand` | **worker-reachable only** (never constructed in `Tooba.Payment.Endpoints`) | `Commands/ReconcileStalePayments/ReconcileStalePaymentsHandler` — real `IRequestHandler<ReconcileStalePaymentsCommand, Result<int>>` | currently dispatched by Host `PaymentReconciliationHostedService` via scoped `ISender`; target owner is `Tooba.Payment.Infrastructure` worker |

Worker reachability = **1**. Endpoint reachability = **16**. Total Payment MediatR requests = **17**.

## 4. C. FluentValidation coverage (exhaustive)

`Tooba.Payment.Tests` / production contain **zero** `AbstractValidator` / `IValidator<` occurrences today → **0/16 validators exist**.

| Request | Classification | Required validator | Status |
| --- | --- | --- | --- |
| `InitiateStorefrontPaymentCommand` | `VALIDATOR_REQUIRED` | `InitiateStorefrontPaymentCommandValidator` | MISSING |
| `GetStorefrontWalletQuoteQuery` | `VALIDATOR_REQUIRED` | `GetStorefrontWalletQuoteQueryValidator` | MISSING |
| `ListStorefrontPaymentMethodsQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` | — (zero-payload request) | n/a |
| `GetStorefrontPaymentQuery` | `VALIDATOR_REQUIRED` | `GetStorefrontPaymentQueryValidator` | MISSING |
| `GetStorefrontPaymentSandboxContextQuery` | `VALIDATOR_REQUIRED` | `GetStorefrontPaymentSandboxContextQueryValidator` | MISSING |
| `CompleteSandboxPaymentCommand` | `VALIDATOR_REQUIRED` | `CompleteSandboxPaymentCommandValidator` | MISSING |
| `SubmitManualPaymentEvidenceCommand` | `VALIDATOR_REQUIRED` | `SubmitManualPaymentEvidenceCommandValidator` | MISSING |
| `RetryManualPaymentCommand` | `VALIDATOR_REQUIRED` | `RetryManualPaymentCommandValidator` | MISSING |
| `RetryUnpaidPaymentCommand` | `VALIDATOR_REQUIRED` | `RetryUnpaidPaymentCommandValidator` | MISSING |
| `UploadManualPaymentProofCommand` | `VALIDATOR_REQUIRED` | `UploadManualPaymentProofCommandValidator` | MISSING |
| `ProcessPaymentWebhookCommand` | `VALIDATOR_REQUIRED` | `ProcessPaymentWebhookCommandValidator` | MISSING |
| `GetAdminPaymentQuery` | `VALIDATOR_REQUIRED` | `GetAdminPaymentQueryValidator` | MISSING |
| `ReconcileAdminPaymentCommand` | `VALIDATOR_REQUIRED` | `ReconcileAdminPaymentCommandValidator` | MISSING |
| `ConfirmAdminDepositCommand` | `VALIDATOR_REQUIRED` | `ConfirmAdminDepositCommandValidator` | MISSING |
| `RejectAdminDepositCommand` | `VALIDATOR_REQUIRED` | `RejectAdminDepositCommandValidator` | MISSING |
| `QueryAdminPaymentsGridQuery` | `VALIDATOR_REQUIRED` | `QueryAdminPaymentsGridQueryValidator` | MISSING |

Totals: **15 `VALIDATOR_REQUIRED` (all MISSING) + 1 `NO_VALIDATOR_REQUIRED_NO_INPUT` = 16**.

| Worker-only request | Classification |
| --- | --- |
| `ReconcileStalePaymentsCommand` | `NO_VALIDATOR_REQUIRED_INTERNAL_WORKER` — constructed only by the Payment-owned worker from Payment-owned options (already clamped at the options boundary); no untrusted payload. |

**Guard gap**: none of the above is inventoried anywhere. No validator-coverage guard exists for Payment.

Shape rules for the future validators (transport shape only; no business duplication): non-empty `Guid`s and non-empty `CheckoutId`/`CartId`; Guid-shaped `PaymentId`/`AttemptId`/`ProofMediaAssetId`; non-blank `ProviderCode`/`ActorUserId`-independent guest secret shape; non-negative `Amount`; 3-character currency shape; non-blank `BodyText`/signature header only through the existing options/verifier; **no** gateway availability, payable-amount, ownership, webhook amount-match, status-transition or DB uniqueness rules in validators.

## 5. D. Physical structure + path↔namespace

Root `.cs` files (all five projects): **none** — `ROOT_ALLOWLIST` is already clean.

| Project | Top-level folders |
| --- | --- |
| `Tooba.Payment.Domain` | `Aggregates`, `Events`, `ValueObjects` |
| `Tooba.Payment.Contracts` | `Admin`, `Customer`, `Events`, `Hold`, `Returns`, `Settlement`, `Storefront` |
| `Tooba.Payment.Application` | `Commands`, `Errors`, `Models`, `Orchestration`, `Ports`, `Queries` |
| `Tooba.Payment.Infrastructure` | `Adapters`, `DependencyInjection`, `Directories`, `Events`, `Messaging`, `Persistence`, `Providers` |
| `Tooba.Payment.Endpoints` | `Admin`, `Errors`, `Storefront`, `Webhooks` |

Path↔namespace findings:

- **No mismatch exists.** A full scan of every non-generated Payment `.cs` found namespace == exact path-derived namespace, with two already-exempt generated cases: `Persistence/Migrations/*` and `PaymentDbContextModelSnapshot.cs` declare `Tooba.Payment.Infrastructure.Persistence.Migrations` (correct for the folder) — already skipped by the current guard.
- `Application/Orchestration/GlobalUsings.cs` declares `global using Tooba.Payment.Application.Orchestration;` — this is a self-import alias that only removes the need for the 9 sibling handlers to add a `using` for their own project sub-namespace. It hides **no** foreign module and compiles no cross-boundary coupling, so it is **not** a `NO_NAMESPACE_ALIAS_WORKAROUND` violation; it must, however, be **explicitly allowlisted with justification** in the ARCH-COMPLETE-002 manifest (Cart precedent: `GlobalUsings.Domain.cs`/`GlobalUsings.Layout.cs` with `rootAllowlistJustification`).
- `Infrastructure/DependencyInjection/PaymentModule.cs` is correctly foldered; its namespace matches path.
- Nested namespaces (`Adapters`, `Providers`, `Directories`, `Messaging`, `Persistence`) all match path exactly.
- **God-file**: `Infrastructure/Directories/PaymentDirectory.cs` = **879 LOC**, single class implementing `IPaymentDirectory, IPaymentReconciliationDirectory, IPaymentAdminDirectory, IPaymentExpiryDirectory` — this is the Payment god-file and a real extraction-readiness liability. It is flagged, not fixed (audit-only).
- No `TypeForwardedTo`, no `OfferGlobalUsings`-style Host alias for Payment.

## 6. E. Cross-module boundaries

| Project | References | Verdict |
| --- | --- | --- |
| `Tooba.Payment.Domain` | `Tooba.BuildingBlocks` | clean |
| `Tooba.Payment.Contracts` | `Tooba.BuildingBlocks` | clean |
| `Tooba.Payment.Application` | `BuildingBlocks`, `Payment.Contracts`, `Payment.Domain`, **`Wallet.Contracts`**, **`Order.Contracts`**, **`Media.Contracts`** | clean — Contracts-only foreign boundaries |
| `Tooba.Payment.Infrastructure` | `Payment.Application`, `Wallet.Contracts`, `Tooba.ModuleContracts`, `Tooba.Persistence` | clean — no foreign Application/Domain/Infrastructure, no Host |
| `Tooba.Payment.Endpoints` | `Payment.Application`, `BuildingBlocks` | clean — no Infrastructure, no Host |

- No `WalletDbContext`, no `using Tooba.Wallet.Application/Domain/Infrastructure` anywhere in Payment production.
- No `Order.Application` / `Order.Infrastructure` / `Order.Domain` / `Media.*` / `Inventory.*` references.
- Payment never references `Tooba.Host`; Host references Payment (correct direction).
- `PaymentHostContractBridge` is **inside** `Tooba.Payment.Infrastructure/Adapters` and implements the Payment-owned `IPaymentAdminGateway`, `IPaymentCustomerGateway`, `IPaymentHoldSettingsGateway` Contracts. Its consumers are Host (`CommerceHoldPolicy`, `HoldPolicySettingsEndpoints`) and other modules' Application layers (Order) through those Contracts — never Host internals. Its name is legacy ("Host" here means "the Host/Order admin surfaces that consume the contracts", as its own doc comment states), not a Host-semantics leak.
  - **Classification: `RENAME_INTERNAL_PAYMENT_BRIDGE`** (e.g. `PaymentContractBridge` / `PaymentContractGateways`). Not an architectural violation, and **not renamed in this audit**.

### Dead / obsolete seams found (extraction-readiness debt)

`Tooba.Payment.Application/Ports/PaymentStorefrontBoundaryPorts.cs` carries four `[Obsolete]` interfaces with **zero production consumers** — they are dead residue that must be deleted during the structure certification:

1. `IStorefrontCheckoutPaymentAccessPort` (+ `StorefrontCheckoutPaymentAccessDto`) — only self-references remain.
2. `IPaymentProofMediaPort`.
3. `IPaymentUnpaidRetrySupplyPort`.
4. `IPaymentAdminOrderEnrichmentPort` — superseded by `Order.Contracts IPaymentAdminOrderEnrichmentReader`, which `QueryAdminPaymentsGridHandler` already injects.

## 7. F. Error / localization / time / id / Result

| Concern | Finding | Verdict |
| --- | --- | --- |
| Result / stable typed errors | `Result`/`Result<T>` + `SemanticError` used across handlers; `PaymentErrorCodes` is the stable code table; endpoints call `api.From(...)` / `api.FromFailure(...)` | OK |
| No `ex.Message` classification | `WalletPaymentGateway.cs:86-88` classifies `ex.Message.StartsWith("wallet.")` / `Contains("payment.wallet")` / `Contains("insufficient")`, and `PaymentDirectory.cs:721-727` catches `InvalidOperationException` by `ex.Message == "payment.refund.gateway.unconfigured"` / `StartsWith("payment.")`. `PaymentExceptionMapper` correctly uses **exact** match. | **GAP — the two `Contains`/`StartsWith` sites are prose heuristics and have no guard coverage** |
| Localization at presentation only | no localized error resource in Payment production; errors surface as codes | OK |
| Persian prose in Domain/Application/Infrastructure | zero user-facing Persian **strings**; only XML-doc/comments are Persian (e.g. `PaymentEvents.cs`, `PaymentDirectory.cs`, `PaymentModule.cs`). One user-facing fallback literal is Persian: `QueryAdminPaymentsGridHandler` uses `order?.CustomerDisplayName ?? "مشتری توبا"` and `"NotApplicable"`/`"—"` fallbacks | **GAP — presentation fallback composed inside the Application handler** |
| `IClock` | injected in `PaymentDirectory`, `WalletPaymentGateway`, `ManualPaymentGateway`, and `ReconcileStalePaymentsHandler` | OK |
| `IIdGenerator` | used by `PaymentDirectory`; endpoint `ResolveIdempotencyKey` uses `IIdGenerator ids` | OK |
| No direct `DateTime.UtcNow` / `DateTimeOffset.UtcNow` / `Guid.NewGuid()` / `UuidV7.New()` | zero hits in production (test hits only) | OK |
| No raw `StartActivity(` | zero hits in production | OK |
| Silent catch | none | OK |

## 8. G. Structure guard quality — `PaymentArchitectureGuardTests` (302 LOC)

Already present: Domain/Endpoints/Contracts/Infrastructure project-reference checks; Wallet-Contracts-only; root-dump checks per project; `PaymentDbContext` Host allowlist; `UtcNow`/`Guid.NewGuid`/`StartActivity` bypass scan; silent-catch scan; localized-prose scan; endpoint `ISender`/no-Directory/no-`ex.Message` checks; Host forbidden-file existence checks; Host `Payment.Application.Ports` boundary scan.

Exact gaps:

1. **Namespace checks use loose `StartsWith`, not exact path-derived equality.** `AssertNamespacesAlign` (line 283) only asserts `ns.StartsWith(nsPrefix)` and `ns.StartsWith(nsPrefix + "." + folder)` (line 301). A file physically under `Adapters/` declaring `Tooba.Payment.Infrastructure.AdaptersXYZ` or a file directly in `Application/` declaring `Tooba.Payment.Application.Orchestration` would pass. Must be replaced with exact equality (`expected == ns`) as done for Offer.
2. **No alias-workaround check.** No assertion rejects `global using` in Payment production or a Host Payment alias.
3. **No `forbiddenRootFiles` enforcement.** The guard derives allowed root files from `EndpointModule.cs` only (line 272) and cannot fail on a specific forbidden root file list.
4. **Root allowlists are not explicit.** `AssertNoRootDump` hardcodes per-project folder lists inline and `EndpointModule.cs` is silently whitelisted for *every* project (not just Endpoints) — no manifest-driven allowlist.
5. **`Application/Orchestration/GlobalUsings.cs` is silently tolerated** — never asserted, never justified, so no guard protects against a future foreign-module global using there.
6. **Validator coverage is entirely absent** — no per-request inventory, no required/not-required classification, no `IValidator<T>` DI resolution proof, no `MediatR 12.5.0` assertion for Payment.
7. **Endpoint-reachable request inventory is not exhaustive.** The guard asserts four sample request type names appear in Application (lines 189-193) and does not prove the endpoint set equals the constructed set.
8. **Host Payment residue is guarded only by presence of two authorizers** and by a handful of forbidden Host files; there is no assertion that Host has no Payment grid whitelist, no Payment reconciliation options/worker, and no Payment command construction.
9. **God-file / oversized-file drift is not guarded** (879-LOC `PaymentDirectory.cs`).
10. **`IPaymentAdminGridQueryNormalizer`** is not asserted to be Payment-owned, and `AdminListGridPolicies` is not asserted to contain no Payment entry.

## 9. H. Proposed Payment manifest (NOT applied in this audit)

```json
{
  "module": "Payment",
  "structureCertified": true,
  "lockVersion": "ARCH-COMPLETE-002",
  "projects": [
    {
      "projectName": "Tooba.Payment.Application",
      "rootAllowlist": [],
      "forbiddenRootFiles": [
        "PaymentContracts.cs",
        "PaymentHandlers.cs",
        "PaymentRequests.cs",
        "PaymentQueries.cs",
        "PaymentQueryHandlers.cs",
        "StorefrontPaymentOrchestrator.cs",
        "PaymentErrorCodes.cs"
      ],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Payment.Endpoints",
      "rootAllowlist": ["PaymentEndpointModule.cs"],
      "forbiddenRootFiles": [
        "PaymentStorefrontEndpoints.cs",
        "PaymentAdminEndpoints.cs",
        "PaymentWebhookEndpoints.cs",
        "IPaymentStorefrontAuthorizer.cs",
        "IPaymentAdminAuthorizer.cs",
        "IPaymentAdminGridQueryNormalizer.cs",
        "PaymentErrorCatalogContributor.cs",
        "PaymentErrorResources.cs",
        "PaymentEndpointLocalizer.cs"
      ],
      "forbiddenTopLevelFolders": []
    },
    {
      "projectName": "Tooba.Payment.Infrastructure",
      "rootAllowlist": [],
      "forbiddenRootFiles": [
        "PaymentModule.cs",
        "PaymentDbContext.cs",
        "PaymentDirectory.cs",
        "PaymentEvents.cs",
        "PaymentOutboxRegistration.cs",
        "PaymentReconciliationWorker.cs",
        "PaymentReconciliationOptions.cs",
        "PaymentGatewayRegistry.cs"
      ],
      "forbiddenTopLevelFolders": []
    }
  ]
}
```

Approved capability folders for the future guard (must equal the live folder set): Application `Commands, Queries, Errors, Models, Orchestration, Ports, Validators`; Endpoints `Admin, Storefront, Webhooks, Errors`; Infrastructure `Adapters, DependencyInjection, Directories, Events, Messaging, Persistence, Providers` (+ the new worker/options home chosen in the residue task).

Additional manifest/SoT requirements to apply with it: certify Payment, remove `Payment` from `uncertifiedHttpOwningModules`, add `Payment` to `structureLock.certifiedModules` (becoming `Order, Cart, StoreContext, Offer, Payment`), and add the `paymentArchComplete002Structure` block.

## 10. I. Deterministic implementation split

**DECISION — `TWO_TASKS`.**

The Host residue repair moves runtime composition (a new `AddHostedService` inside `PaymentModule`, new config binding, Host `Program.cs` line deletions, a new Payment-owned grid policy + Host grid-policy deletion, four obsolete-interface deletions). The structure certification is validator/namespace/manifest work. They touch independent blast radii and the residue task must land first so the structure guard can assert its invariants.

### Task 1 — `TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001` (Host residue ownership repair)

Exact files:

1. `src/backend/Host/Tooba.Host/PaymentReconciliationHostedService.cs` — **move** into `Tooba.Payment.Infrastructure`; rename to `PaymentReconciliationWorker.cs`; keep generic seams only; Home the command construction, timing policy and telemetry inside Payment.
2. `src/backend/Host/Tooba.Host/PaymentReconciliationHostOptions.cs` — **move** into Payment (options type with `SectionName`).
3. `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs` — register `Configure<…>` + `AddHostedService<…>`.
4. `src/backend/Host/Tooba.Host/Program.cs` — remove lines 103 and 142; remove now-unneeded usings.
5. `src/backend/Host/Tooba.Host/Admin/HostPaymentAdminGridQueryNormalizer.cs` — **delete**.
6. `src/backend/Host/Tooba.Host/Grid/AdminListGridPolicies.cs` — remove the `Payments` policy + its `AdminReceiptListItem` coupling.
7. `src/backend/Host/Tooba.Host/Admin/AdminPanelModels.cs` — drop the payments row model if it becomes unused.
8. `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/…` — add the Payment-owned admin payments grid policy + default normalizer implementation.
9. `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentStorefrontBoundaryPorts.cs` — delete the four `[Obsolete]` dead ports + `StorefrontCheckoutPaymentAccessDto`.
10. `src/backend/Host/Tooba.Host.Tests/HostFolderStructureTests.cs` — remove the two Payment root allowlist entries.
11. `src/backend/Host/Tooba.Host.Tests/Architecture/HostCartResidualGuardTests.cs` — remove the two Payment worker allowlist entries.
12. `src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs` — flip the Host residue assertions to assert absence.
13. `src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentR1CqrsContractTests.cs` — keep worker/reconcile coverage green after the move.
14. `src/backend/Modules/Order/Tooba.Order.Tests/Architecture/OrderAdminOperationsArchitectureGuardTests.cs` — repath the `PaymentHostContractBridge` rename reference (line 250) if the bridge is renamed.

### Task 2 — `TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001` (structure certification)

Exact files:

1. `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/PaymentValidationCodes.cs` (new)
2. `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/PaymentFluentRules.cs` (new)
3. 15 validator files under `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/` (new) — one per `VALIDATOR_REQUIRED` request in §4.
4. `src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs` — replace `StartsWith` namespace checks with exact equality; add alias-workaround rejection; add explicit per-project root allowlists + `forbiddenRootFiles`; keep migrations exemption.
5. `src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentEndpointValidatorCoverageGuardTests.cs` (new) — 16 endpoint requests, 15 required validators, 1 `NO_VALIDATOR_REQUIRED_NO_INPUT`, 1 worker `NO_VALIDATOR_REQUIRED_INTERNAL_WORKER`, `MediatR 12.5.0`, `ISender`-only.
6. `docs/architecture/tmar-module-structure-manifests.json` — apply §9.
7. `docs/architecture/tmar-current-state.json` — certify Payment + `paymentArchComplete002Structure` block.
8. `src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs` and `.../Architecture/TmarCompleteReferenceStructureGateTests.cs` — expected certified set + Payment SoT coherence.
9. `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` and `TOOBA-ARCHITECT-BOOTSTRAP.md` — certification record + next task/list.
10. `docs/evidence/TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001/payment-structure-certification.md` (new)

No discovery remains: every request, validator, folder, reference and Host call site is enumerated above with a settled decision.

## 11. Certification / preservation state

- Payment: `COMPLETE_REFERENCE_PATTERN` / `HTTP_OWNING` / `MODULE_ENDPOINTS` / `MEDIATR_12_5`, **still not** `ARCH-COMPLETE-002` certified.
- Cart / Order / StoreContext / Offer certifications unchanged.
- `Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`; frontend untouched.
- Settlement / Fulfillment / Returns / Notification / Support / Wallet / Promotion certifications untouched; no unrelated Host module audited.

## 12. Residual defects

- No confirmation of the Host Payment production behavior beyond static inspection (audit-only; no runtime probe performed, per protocol no placeholder payloads were posted).
- `PaymentDirectory.cs` (879 LOC, 4 interfaces) god-file remains — flagged for extraction, not fixed.
- The two `ex.Message.Contains/StartsWith` sites (§7) remain unguarded until the structure task adds the guard.
- `PaymentHostContractBridge` naming remains until explicitly renamed.

## 13. Validation

- `Tooba.Payment.Tests` / `PaymentArchitectureGuardTests` — PASS (unchanged; no Payment production code changed).
- `Tooba.Payment.Tests` CQRS contract tests (`PaymentR1CqrsContractTests`, `PaymentFinancialCharacterizationTests`, `CustomerPaymentFactoryBehaviorTests`) — PASS.
- `TmarDurableGuardTests`, `TmarCompleteReferenceStructureGateTests` — PASS after the recovery stamp update.
- No broad suite; no full build beyond what recovery-guard compilation required.
