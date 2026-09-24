# TB-TMAR-PAYMENT-PRECERT-HYGIENE-001 — Payment pre-certification extraction & semantic hygiene

## Parent acceptance

| Item | Value |
| --- | --- |
| Parent task | TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1 |
| Builder/parent acceptance commit | `f43904967311a3ac3e3f2cca773af3c733458a76` |
| Recovery stamp commit | `62e016d5f0bd623057e4d8abf3050005b2896c90` |
| Parent state | ACCEPTED_AFTER_R1_BEHAVIOR_PARITY_REPAIR |

Verified at start of this task: reconciliation effective minimum = 15 s; all Payment admin grid structural
failures converted `GridQueryValidationException` → stable `SemanticException`; Host Payment runtime/grid
residue removed; only `HostPaymentAdminAuthorizer` + `HostPaymentStorefrontAuthorizer` remain; Payment → Host
dependency zero; Payment NOT ARCH-COMPLETE-002 certified.

## A. Dead obsolete Payment ports removed

File: `src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentStorefrontBoundaryPorts.cs`

Exact reference check (repo-wide, excluding `bin`/`obj`) proved all of the following members had **zero**
production consumers, only self-references inside the declaring file:

- `StorefrontCheckoutPaymentAccessDto`
- `IStorefrontCheckoutPaymentAccessPort`
- `IPaymentProofMediaPort`
- `IPaymentUnpaidRetrySupplyPort`
- `IPaymentAdminOrderEnrichmentPort`
- `AdminPaymentOrderEnrichmentDto` (also zero production consumers — deleted)

Live members preserved unchanged:

- `ICheckoutActorPolicyPort`
- `IPaymentGatewayCatalogPort`
- `IPaymentWebhookSignatureVerifier`

No replacement seam invented. No behaviour change.

## B. Legacy internal bridge renamed

| Before | After |
| --- | --- |
| `Tooba.Payment.Infrastructure/Adapters/PaymentHostContractBridge.cs` | `Tooba.Payment.Infrastructure/Adapters/PaymentContractBridge.cs` |
| class `PaymentHostContractBridge` | class `PaymentContractBridge` |

Namespace unchanged: `Tooba.Payment.Infrastructure.Adapters`. Still implements all three contract gateways:
`IPaymentAdminGateway`, `IPaymentCustomerGateway`, `IPaymentHoldSettingsGateway`.

`PaymentModule` registers `PaymentContractBridge` and resolves the three contract interfaces from it. No
type-forwarding, no compatibility alias, no obsolete shim — the old file/type no longer exist.

References updated exactly: `PaymentModule.cs`, `OrderAdminOperationsArchitectureGuardTests.cs` (adapter path).

## C. Typed refund fault (Payment)

- `FailClosedPaymentRefundGateway` now throws `ContractOperationException("payment.refund.gateway.unconfigured")`
  (previously `InvalidOperationException` with the code in `Message`).
- `PaymentDirectory.CloseOrStartRefundForOrderCancelAsync` catches **typed** faults only:
  - `catch (ContractOperationException ex) when (ex.Code == "payment.refund.gateway.unconfigured")` → preserves
    `RefundPending` for admin action.
  - `catch (ContractOperationException ex) when (ex.Code.StartsWith("payment.", StringComparison.Ordinal))` →
    `payment.MarkRefundFailed(ex.Code, _clock.UtcNow)` — classification by `Code`, never `Message`.
- Unknown exceptions propagate unchanged (no arbitrary `InvalidOperationException` text matching).

## D. Typed Wallet boundary at the Payment↔Wallet contract

- `WalletDirectory.SpendForOrderPaymentAsync` (the `IWalletOrderPaymentPort` implementation surface) now throws
  `ContractOperationException(stableCode)` for its expected rejection paths. Stable code values are unchanged:
  `wallet.rejected.2YfZiNuM`, `wallet.rejected.2YXYqNmE`, `wallet.rejected.SWRlbXBv`,
  `wallet.rejected.2qnZhNuM`, `wallet.rejected.2K3Ys9in`, `wallet.rejected.2KfYsdiy`,
  `wallet.rejected.2YXZiNis`.
- No unrelated Wallet use case redesigned; no Wallet persistence/domain change; unrelated throw sites untouched.
- `WalletPaymentGateway.VerifyAsync` now catches `ContractOperationException` and returns
  `GatewayVerification(false, null, "WALLET_SPEND_REJECTED")`. Message heuristics
  (`StartsWith("wallet.")`, `Contains("payment.wallet")`, `Contains("insufficient")`) removed. Unknown faults are
  **not** swallowed — they still propagate.

## E. Localized fallback removed from Payment Application

File: `src/backend/Modules/Payment/Tooba.Payment.Application/Queries/QueryAdminPaymentsGrid/QueryAdminPaymentsGridQuery.cs`

| Before | After |
| --- | --- |
| `order?.CustomerDisplayName ?? "مشتری توبا"` | `order?.CustomerDisplayName ?? string.Empty` |
| `order?.ReservationLabel ?? "—"` | `order?.ReservationLabel ?? string.Empty` |
| `order?.ReservationLabelEn ?? "—"` | `order?.ReservationLabelEn ?? string.Empty` |

Machine-state values `"NotApplicable"` / `"none"` were retained as stable semantic values. The
`AdminPaymentGridItemDto` record defaults for the reservation labels were also made locale-neutral
(`""` instead of `"—"`). No English UI prose was introduced into Application as a replacement.

## F. PaymentDirectory size

| Measurement | Value |
| --- | --- |
| Physical LOC before (HEAD of this task) | 971 |
| Physical LOC after | 971 |
| Net expansion | 0 |

The typed-fault cleanup was line-neutral (`InvalidOperationException`/`.Message` → `ContractOperationException`/`.Code`
on the same three lines). No split was performed (explicitly out of scope). A guard was added asserting
`PaymentDirectory.cs` cannot grow beyond the accepted 971-line baseline.

## G. Architecture guards strengthened

`src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs`:

- `Payment_precert_hygiene_removes_dead_ports_and_legacy_bridge_name` — old bridge file/type absent,
  `PaymentContractBridge` present and preserving the three contract interfaces, no dead port types remain,
  `PaymentModule` registers the new bridge.
- `Payment_expected_faults_use_typed_codes_not_message_classification` — typed refund throw, typed directory
  catch by `Code`, typed Wallet boundary, and **no** `.Message.Contains(` / `.Message.StartsWith(` / `.Message ==`
  anywhere in Payment production sources.
- `Payment_application_admin_grid_has_no_localized_presentation_fallback` — empty-string fallbacks, no Persian
  literal in the admin grid query, DTO defaults locale-neutral.
- `PaymentDirectory_does_not_expand_beyond_accepted_baseline` — size freezing.
- Pre-existing guards retained/unchanged (zero Payment → Host dependency, Host residue = two security adapters).

## H. Focused tests added/updated

New: `src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentPrecertHygieneTests.cs`

- fail-closed refund gateway throws typed `ContractOperationException` with code
  `payment.refund.gateway.unconfigured`;
- order-cancel refund path preserves `RefundPending` on that typed code;
- other typed refund failure uses `Code` (`RefundFailed` with the code), not `Message`;
- unknown refund exception is not swallowed and leaves state untouched;
- Wallet order-payment expected rejection is typed at the boundary;
- `WalletPaymentGateway` converts typed Wallet rejection to `WALLET_SPEND_REJECTED`;
- unknown Wallet exception is not swallowed;
- admin grid missing customer enrichment returns empty display string; missing reservation labels return empty
  strings;
- bridge rename preserves all three Contracts interfaces.

Updated: `WalletFinancialCharacterizationTests.cs` — insufficient-balance rejection now asserts the typed
`ContractOperationException.Code`. `WalletCheckoutRefundTests.cs` (Host) — expected Wallet order-payment
rejections now assert the typed exception.

No broad Wallet suite was run beyond the smallest focused Wallet order-payment boundary tests.

## Validation

| Check | Result |
| --- | --- |
| `dotnet test src/backend/Modules/Payment/Tooba.Payment.Tests` | PASS (50/50) |
| `dotnet test src/backend/Modules/Wallet/Tooba.Wallet.Tests` | PASS (24/24) |
| Host `TmarDurableGuardTests` + `TmarCompleteReferenceStructureGateTests` + `HostCartResidualGuardTests` + `WalletCheckoutRefundTests` | PASS (23/24, 1 skipped — Docker/Testcontainers unavailable) |
| `dotnet build src/backend/Tooba.slnx` | PASS — 0 errors |

Pre-existing, out-of-scope: `TmarSourceSizeAndInfraAppTests` (source-size baseline drift and two
Order.Infrastructure → foreign Application edges) fails identically on the pristine tree and is unrelated to this
task; not touched, not weakened.

## PASS criteria

| Criterion | State |
| --- | --- |
| Dead obsolete Payment ports gone | YES |
| Old `PaymentHostContractBridge` file/type gone | YES |
| `PaymentContractBridge` preserves contract behaviour | YES |
| Payment refund path has no Message classification | YES |
| `WalletPaymentGateway` has no Message classification | YES |
| Expected Wallet order-payment failures cross as typed `ContractOperationException` | YES |
| Payment Application contains no localized Persian fallback in admin grid composition | YES |
| `PaymentDirectory` does not expand | YES (971 → 971) |
| Payment → Host dependency remains zero | YES |
| Host Payment residue stays security-adapters-only | YES |
| Payment remains NOT structure-certified | YES |
| Checkout/frontend unchanged | YES (PAUSED_AT_SAFE_W5_CHECKPOINT; frontendFrozen = true) |
| Focused tests pass | YES |
| Full build passes | YES |
| Recovery points directly to Payment structure certification | YES |

## Recovery state

- `Payment pre-cert hygiene = DEAD_PORTS_REMOVED_TYPED_FAULTS_NO_LOCALIZED_APPLICATION_FALLBACK`
- `internal bridge = PAYMENT_CONTRACT_BRIDGE`
- `Message classification = REMOVED`
- `Payment Host residue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY`
- `Payment structure certification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`
- `nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001`
