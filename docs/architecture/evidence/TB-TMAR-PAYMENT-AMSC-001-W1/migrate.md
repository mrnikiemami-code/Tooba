# TB-TMAR-PAYMENT-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Scope

`src/backend/Modules/Payment/Tooba.Payment.*` — W1 Migrate wave of the AMSC re-standardization, consuming the W0 Analyze verdict `READY_TO_MIGRATE` (commit `6839bb4a`) and repairing the five findings recorded in `docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W0/analyze.md`. The prior ARCH-COMPLETE-002 certification (`3e403aaf`) and the Host-evacuation lineage are preserved as the accepted baseline. Behavior is preserved; only ownership of the stable-code identity, the fault-mapping mechanism and the localization surface change.

## Repair 1 — Stable-code ownership relocation (Contracts boundary)

- Deleted `Tooba.Payment.Application/Errors/PaymentErrorCodes.cs`; the `Application/Errors/` folder is removed entirely.
- Created `Tooba.Payment.Contracts/Errors/PaymentErrorCodes.cs` (`namespace Tooba.Payment.Contracts.Errors`) declaring the module's stable machine codes with the certified declared-code guard:
  - `private static readonly HashSet<string> KnownCodes` (ordinal) seeded from the declared constants.
  - `public static bool IsKnown(string? code)` — whitespace/null false, exact ordinal membership only.
  - `public const string` members keep the identical wire values (no code value changes).
- The four legacy `PaymentExceptionMapper.PublicAliases` source literals (`payment.not_found`, `payment.tracking_reference.required`, `payment.unpaid.retry.invalid_state`, `inventory.supply.unavailable`) are now **real declared codes** (the first three are canonicalized by the existing declared members; `SupplyUnavailable = "inventory.supply.unavailable"` is declared as a consumed Inventory code). Declared count is **28**.
- Descriptor ownership stays honest and unchanged: 24 Payment-owned descriptors registered by `PaymentErrorCatalogContributor`; `inventory.reservation.retry_limit_reached` (Order owner) and `admin.authorization.denied` + `checkout.authentication_required` (Foundation owner) remain deliberately consumed without re-registration.
- All consumers repointed: 17 Application files, `Endpoints/Errors/PaymentErrorCatalogContributor.cs`, `Endpoints/Storefront/PaymentStorefrontEndpoints.cs`, `Domain/Aggregates/PaymentAttempt.cs`, `Infrastructure/Directories/{PaymentDirectory,PaymentAdminDirectory,PaymentExpiryDirectory}.cs`.
- `Tooba.Payment.Domain.csproj` now references its **own** `Tooba.Payment.Contracts` (the canonical `Contracts/Errors` home). This is own-module layering (the same accepted pattern as BulkInquiry `Domain → Contracts`), not cross-module coupling. The architecture guard was tightened so the only permitted `Contracts` reference is `Tooba.Payment.Contracts`; any foreign module `Contracts` remains a violation.

## Repair 2 — Canonical typed-fault seam

- Created `Tooba.Payment.Application/Composition/PaymentOperation.cs` mirroring the certified Media/Inventory/Notification/OperatorProfile/PageComposition/Party shape exactly:
  - `ExecuteAsync<T>(Func<Task<T>>)` and value-less `ExecuteAsync(Func<Task>)`.
  - `ArgumentNullException.ThrowIfNull`.
  - `catch (ContractOperationException ex) when (PaymentErrorCodes.IsKnown(ex.Code))` → `Result.Failure<T>(new SemanticError(ex.Code))`.
  - `catch (SemanticException ex)` → `Result.Failure<T>(ex.Error)`.
  - Unknown codes and unknown exceptions propagate untouched to the canonical global exception boundary.
- Classification is by typed code only — never by message/prose.

## Repair 3 — Retire the legacy parallel mapper

- Deleted `Tooba.Payment.Application/Errors/PaymentExceptionMapper.cs` (the second fault-mapping mechanism with the `PublicAliases` dictionary that silently rewrote four wire codes and the `TryMapExact(string? message, out ...)` overload that classified `InvalidOperationException.Message`).
- Repointed all 15 call sites (13 handler/query leaves + 2 orchestrator sites) to `PaymentOperation.ExecuteAsync`.
- The 11 domain/infrastructure throw sites that emitted the pre-alias wire literals are repointed to the declared constants (identical string values, so no behavior change):
  - `PaymentDirectory.AlreadySucceeded()` now returns `ContractOperationException(PaymentErrorCodes.AlreadySucceeded)` (was `InvalidOperationException("payment.already_succeeded")`).
  - `PaymentDirectory` four `payment.not_found` throws → `PaymentErrorCodes.Missing`.
  - `PaymentAdminDirectory` `payment.tracking_reference.required` → `PaymentErrorCodes.TrackingRequired`.
  - `PaymentExpiryDirectory` `payment.not_found` → `PaymentErrorCodes.Missing`, `payment.unpaid.retry.invalid_state` → `PaymentErrorCodes.UnpaidRetryInvalid`.
  - `PaymentAttempt` `payment.tracking_reference.required` → `PaymentErrorCodes.TrackingRequired`.
- `StorefrontPaymentOrchestrator` `InvalidOperationException(PaymentErrorCodes.*)` sites converted to `ContractOperationException(PaymentErrorCodes.*)`; `RetryUnpaidCoreAsync` now guards with `PaymentErrorCodes.IsKnown(ex.Code)`.

## Repair 4 — Missing localization surface

- Created `Tooba.Payment.Contracts/Errors/PaymentErrorResourceSet.cs` (`IErrorResourceSet` owning the `payment.` and `admin.payment.` keyspaces) with a `PaymentErrorResources` `ResourceManager` marker.
- Created bilingual `Tooba.Payment.Contracts/Resources/PaymentErrors.resx` and `PaymentErrors.fa.resx` with 24 entries (one per registered Payment-owned descriptor key).
- Registered the set in `PaymentEndpointModule.AddPaymentEndpointPresentation()` (`services.AddSingleton<IErrorResourceSet, PaymentErrorResourceSet>()`).
- Because the composed `ResourceErrorMessageLocalizer` resolves the first owning set, Order's `payment.*` keys remain intact while Payment now owns its own identical bilingual text. The migration of `payment.*` ownership cannot silently regress.

## Repair 5 — No further repair required

Logging, telemetry, correlation, persistence and schema are verified canonical/unchanged by W0; zero logging/telemetry/correlation/persistence/schema changes were made in W1. No migration file was touched.

## Verification

- `dotnet build` of `Tooba.Payment.Tests` and `Tooba.Host.Tests`: succeeded (0 errors).
- `Tooba.Payment.Tests`: **96 passed / 0 failed**.
- `Tooba.Host.Tests` Payment/Storefront/Wallet/Unpaid filter: **103 passed / 3 skipped**.
- New durable guard `PaymentModuleAmsc001W1MigrateGuardTests`: **10 passed / 0 failed**, pinning the Contracts error home, the declared-code count + `IsKnown`, the retired mapper and alias literals, the bilingual resource pair, the resource-set registration and the Contracts-only boundary.
- Two unrelated pre-existing failures remain in `HostStorefrontAmcR1GuardTests` / `HostStorefrontAmcR3GuardTests` (missing Wishlist/Catalog module files, not Payment); they are outside this wave's scope and unchanged by it.

## State

- Localization-State: `MISSING_INFRASTRUCTURE_USE` → `OWNED_BY_PAYMENT`.
- Stable-Error-Code-State: `CATALOGUED_27_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED` → `CONTRACTS_OWNED_28_DECLARED_24_REGISTERED_3_INTENTIONALLY_CONSUMED_ISKNOWN_GUARDED`.
- Contracts-Boundary-State: `VIOLATION` → `COMPLIANT`.
- `microserviceExtractable`: `TARGET_TRUE_BLOCKED_...` → `TRUE_LOCALIZATION_AND_STABLE_CODE_OWNERSHIP_REPAIRED`.
- Verdict: `READY_TO_STRUCTURE`.
