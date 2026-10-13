# TB-P0-CHECKOUT-PROVIDER-ALIGNMENT-027 — analysis

Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-FINAL-CLICK-API-026
Expected-Base-SHA: d6dde48974f5b4c315aa868179c33d7df7be9da7
Status: IMPLEMENTED_SAFE_BOUNDED_SLICE (blocker #2 of 026 closed in production code)

## 1. The defect (blocker #2 of task 026), restated with exact anchors

For the neutral storefront selector `gateway` (`PaymentProviderCodes.GatewayCatalog`), the 019 composer passed the
**bare alias** to 018 hold preparation while Payment resolved the alias to a **concrete provider**:

- 018 policy lookup keyed by the raw selector string:
  `OrderPaymentHoldPreparationService` → `CommerceHoldPolicySource.FindMethod(providerCode)` (dictionary keyed by the
  trimmed provider code).
- Payment's own unpaid timeout keyed by the concrete provider:
  `StorefrontPaymentOrchestrator.InitiateAsync` assigns `providerCode = _catalog.DefaultProvider` for the `gateway`
  alias (`.../Storefront/Orchestration/StorefrontPaymentOrchestrator.cs:124,149,151`), and
  `PaymentUnpaidTimeoutAssigner` then calls `ResolveUnpaidTimeoutAt(payment.ProviderCode, now)`.

Result: a per-provider hold override configured for the concrete provider applied to Payment but not to the 018 hold,
so the reservation hold could be **shorter** than the selected provider's policy (undersized hold), and it could be
provably inconsistent with the operator's intent.

A second, symmetric hole existed for **unknown non-empty selectors** (e.g. `someprovider`): the orchestrator's
`else if` chain matches none of its branches, so `providerCode` silently keeps `_catalog.DefaultProvider` **without**
running the `IsOnlineGatewayOffered()` check — an opaque fall-through to a different provider than the one the caller
named, while 018 would have prepared the hold under the caller's string.

## 2. The fix (production code, not docs)

A minimal additive **Payment.Contracts** resolver, owned and implemented by Payment, consumed by Order through
Contracts only:

- New port: `src/backend/Modules/Payment/Tooba.Payment.Contracts/Storefront/ProviderSelectionContracts.cs`
  - `StorefrontProviderSelection(string RequestedSelector, string CanonicalProviderCode)`
  - `IPaymentProviderSelectionPort.ResolveAsync(string? requestedProviderCode, bool useWallet, CancellationToken)`
    → `null` means **fail closed**.
- New implementation (Payment authority over the same catalog the orchestrator uses):
  `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentProviderSelectionAdapter.cs`
  - `wallet` decision → `PaymentProviderCodes.Wallet`.
  - `manual` → `PaymentProviderCodes.Manual` **only if** `IPaymentGatewayCatalogPort.ManualCardToCardEnabled`.
  - `gateway` alias → `IPaymentGatewayCatalogPort.DefaultProvider` **only if**
    `IPaymentGatewayCatalogPort.IsOnlineGatewayOffered()`, and only if the concrete default is non-blank.
  - absent/unknown selector → `null` (never silently remapped).
  - No hardcoded provider name, duration, fallback hour or TTL is introduced; the sole source of the concrete provider
    is the already-existing catalog authority.
- DI: `PaymentModule.AddServices` registers
  `services.AddScoped<IPaymentProviderSelectionPort, PaymentProviderSelectionAdapter>();` next to
  `IPaymentGatewayCatalogPort` (its only dependency).

Composer change (`StorefrontStartPaymentComposer`):

1. **Before** any commit/replay, the composer resolves the selector via `IPaymentProviderSelectionPort`. Resolution is
   "step 2" of the documented order, immediately after syntactic validation.
   - `null` → typed reject `StorefrontStartPaymentFault.ProviderUnavailable` with outcome `InvalidRequest`:
     **zero** commit, **zero** hold preparation, **zero** money.
2. 018 receives `selection.CanonicalProviderCode` — the composer never passes bare `gateway` to preparation.
3. Payment initiation still receives the **trusted original selector** (`request.ProviderCode`), so Payment keeps its
   authoritative eligibility/method/idempotency checks and the 021 same-key conflict semantics.

## 3. Why the original selector is preserved at Payment initiation (task's explicit escape hatch)

Passing an already-concrete code into `IPaymentInitiationPort` would land in the orchestrator's opaque fall-through
branch (non-`manual`, non-`gateway`, non-empty) and therefore **skip** `IsOnlineGatewayOffered()`. It would also have
`_gateways.Resolve(concreteCode)` risk if the configured default were not a registered gateway. Preserving the
selector keeps Payment as the authoritative offering/eligibility authority; the composer's only added duty is to make
the *hold* use the same provider.

Equality is structural, not duplicated policy: both the resolver and the orchestrator read the **same**
`IPaymentGatewayCatalogPort` member (`DefaultProvider`, guarded by `IsOnlineGatewayOffered()`), and the Payment-module
test `The_single_provider_authority_is_the_orchestrator_catalog_and_the_adapter_reuses_it` pins that single source in
both files and forbids a second literal provider/duration in the adapter.

## 4. Fail-closed semantics (explicitly stricter than the direct route on one input)

The composer refuses an **absent** selector (`null`/blank with `UseWallet=false`) instead of falling back to the
default online gateway, because the mission lists "absent method" as a threat to defend and demands fail-closed on an
unavailable/disallowed selector. The published direct payment route is unchanged: it still maps an absent selector to
the default provider after its own offering check. This divergence is intentional, inert (the composer still has no
endpoint) and does not weaken any existing route.

## 5. What is NOT touched

- No endpoint published, no frontier change; the 019 inertness guard is untouched.
- 019 guard `No_module_or_host_route_publishes_start_payment` is a **temporary inertness-phase guard** and, as the
  task requires, is recorded here as needing transition — in the future API publication task only — to assert exactly
  ONE approved Order storefront final-click route and ZERO Host routes. It was neither weakened nor edited here, because
  no route is published yet. The same applies to the `ISender`-only storefront route guard, which needs a targeted
  contract transition at that future step (not blanket deletion).
- No Host / Host.Tests edit, build, test, stop, restart or deploy; no live DB write; no PSP/wallet transaction; the
  Host at :5088 was untouched.
- Pre-existing unrelated dirty/untracked paths were preserved.

## 6. Verification (focused module tests only)

- `Tooba.Payment.Tests`: **150 passed / 0 failed** (full module), including 11 new cases in
  `Behavior/PaymentProviderSelectionAdapterTests.cs`.
- `Tooba.Order.Tests`: **232 passed / 0 failed** (full module); the focused `StorefrontStartPayment` filter is
  **33 passed / 0 failed**, covering the revised gateway test and the new pre-commit fail-closed test.
- No Host runtime proof is claimed from fakes, per the task.

## 7. Remaining blocker status

- 026 blocker **#2: closed** (production code).
- 026 blocker **#1: still open** — the accepted 019 guard forbids the `start-payment` route string in Order.Endpoints
  and Host, so no endpoint may be published until the Architect authorizes its transition (section 5).
