# TB-P0-CHECKOUT-START-PAYMENT-S2-012R — bounded implementation evidence

Task: Order-owned cart-to-checkout final click, Payment-owned initiation exposed to Order only
through a minimal `Tooba.Payment.Contracts` seam (Architect decision S2).
Mode: `BOUNDED_IMPLEMENTATION — STOP_FOR_ARCHITECT`, 20-minute timebox.

Verdict: **BLOCKED_ARCHITECT_DECISION** — the authorized Contracts seam is delivered and green;
the `POST /v1/storefront/cart/{cartId:guid}/start-payment` endpoint is **not** published because the
payability/initiation invariant cannot be enforced with existing seams.

## 1. Boundary facts (verified, not assumed)

| Fact | Evidence |
| --- | --- |
| Order commit opens its **own** `TransactionScope` internally and exposes no post-commit hook | `src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/CheckoutProcessManager.cs` (`using var scope = new TransactionScope(...)` inside `SubmitAsync`) |
| Payability (`ReservationCyclePayabilityPolicy.IsActive` / `IReservationCycleDirectory`) is Order-Application-owned | `Tooba.Order.Application/ReservationCycle/Contracts/ReservationCycleContracts.cs:119,176,182` |
| `StorefrontPaymentOrchestrator.InitiateAsync` never reads reservation-cycle payability | `Tooba.Payment.Application/Storefront/Orchestration/StorefrontPaymentOrchestrator.cs:110` — parameters are `checkoutId, cartId, guestSecret, idempotencyKey, useWallet, providerCodeOverride, authenticatedUserId` only |
| The Order→Payment payability image carries **no** cycle state | `Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs:32` — `PayableCheckoutSnapshot(CheckoutId, Mode, Currency, SellerOrders, ShippingAmount)` |
| `StorefrontPaymentOrchestrator` is registered by the **Host**, not by `PaymentModule` | `src/backend/Host/Tooba.Host/Program.cs:221` |

Consequence: any Order-side flow would be "check cycle in Order, then begin money movement in
Payment" with no atomic or Payment-enforced link between the two. The task forbids asserting that
this is safe, so the endpoint was withheld rather than shipped half-safe.

## 2. What was implemented (safe, additive, no behavior change)

### 2.1 `Tooba.Payment.Contracts/Storefront/PaymentInitiationContracts.cs` (new)

```csharp
public sealed record StorefrontPaymentInitiationRequest(
    Guid CheckoutId, Guid CartId, string? GuestSecret, string IdempotencyKey,
    bool UseWallet, string? ProviderCode, Guid? AuthenticatedUserId);

public sealed record StorefrontPaymentInitiationSnapshot(
    Guid PaymentId, Guid AttemptId, Guid CheckoutId, string Status, string ProviderCode,
    string ProviderRequestReference, string RedirectUrl, decimal Amount, string Currency,
    bool RequiresPspRedirect = true);

public interface IPaymentInitiationPort
{
    Task<StorefrontPaymentInitiationSnapshot> InitiateForCheckoutAsync(
        StorefrontPaymentInitiationRequest request, CancellationToken cancellationToken);
}
```

The request mirrors exactly the canonical `InitiateStorefrontPaymentCommand` inputs
(`Tooba.Payment.Application/Storefront/Commands/InitiateStorefrontPaymentCommand.cs`). No amount
is accepted from the caller — the customer never chooses the amount.

### 2.2 `Tooba.Payment.Infrastructure/Adapters/PaymentInitiationAdapter.cs` (new)

Delegates to the single canonical `StorefrontPaymentOrchestrator.InitiateAsync` and maps the result
one-to-one into the Contracts snapshot. No provider routing, actor policy, eligibility, idempotency
or persistence logic is duplicated; Payment faults (`payment.*`) pass through untouched.

### 2.3 `PaymentModule.AddServices` (modified, +7 lines)

```csharp
services.AddScoped<IPaymentInitiationPort>(sp => new PaymentInitiationAdapter(
    () => sp.GetRequiredService<StorefrontPaymentOrchestrator>()));
```

Deferred `Func<>` resolution follows the module's existing precedent (`Func<IPaymentAdminDirectory>`
in `PaymentDirectory`, see the comment at `PaymentModule.cs:52-54`). It keeps module DI validation
independent of the Host-owned orchestrator registration — without the deferral,
`PaymentPrecertHygieneTests.Directory_ports_resolve_to_distinct_focused_implementations` (which runs
`ValidateOnBuild = true` on the module graph alone) fails, which is exactly the guard that caught the
first, non-deferred attempt.

No Host, Endpoints, Application or schema file was touched. No endpoint exists yet, so no caller can
reach the port.

## 3. Tests run (one module test project, no repeats)

```
dotnet test src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj
Passed! - Failed: 0, Passed: 112, Skipped: 0, Total: 112 — Tooba.Payment.Tests.dll (net8.0)
```

New focused file `Tooba.Payment.Tests/Behavior/PaymentInitiationPortTests.cs` (5 tests):

1. `Contracts_snapshot_carries_every_identity_money_and_redirect_field_unchanged` — no field
   fabricated, dropped or redefined.
2. `Contracts_snapshot_does_not_invent_psp_redirect_when_orchestrator_declines_it` — flags are
   reported, not assumed.
3. `Mapping_fails_closed_when_orchestrator_result_is_missing`.
4. `Adapter_delegates_only_to_the_canonical_orchestrator_and_exposes_no_second_path` — the adapter
   implements the Contracts port, its only dependency is the canonical orchestrator, and it exposes
   exactly one public operation.
5. `Payment_module_registers_the_contracts_initiation_port` — resolves the port from the module's own
   registrations without the Host graph.

The remaining 107 pre-existing Payment tests (including the DI/runtime precert hygiene proof) stay
green: no guard, baseline or locked rule was relaxed.

## 4. Explicitly UNCOVERED / UNVERIFIED

- `POST /v1/storefront/cart/{cartId:guid}/start-payment` — **not implemented**. Therefore none of the
  five required endpoint behaviours (actor rejection, one-shot success, same-key replay after commit,
  initiation-failure retry on the same checkout, fail-closed expired cycle) are verified here.
- `Contract_bridge`-style end-to-end proof that Order commit + initiation compose under one request:
  absent by design (endpoint withheld).
- Real PostgreSQL atomicity: `CheckoutOrderFoundationTests` still cannot run because the running
  Development Host locks the output DLLs. Per the task, the Host was **not** stopped and the build was
  **not** retried in a loop. This remains UNVERIFIED runtime evidence; no cross-module
  `TransactionScope` proof is claimed from source inspection alone.
- The 1-minute dev override vs two-hour payment timeout distinction was not encoded anywhere (no
  durations were introduced or changed in this task).

## 5. Precise proposed remedy for the withheld endpoint

Order of guards required by the task is: successful commit → active-cycle payability → initiation.

- commit + cart replay are already single-shot and owner-checked:
  `CheckoutProcessManager.SubmitAsync` re-uses `FindExistingCheckoutAsync`
  (idempotency key **or** cartId), then `EnsureCheckoutAccess` (buyer party / placed-by user) and
  `ReconcileCartConversionAsync`; a converted cart therefore replays its own persisted checkout and a
  different actor fails closed. Re-using `StorefrontShippingService.CommitAsync` would inherit this
  behaviour instead of re-implementing reservation.
- the unreachable part is "active-cycle payability → begin money movement" as a single invariant.
  A caller-side check in Order cannot be proof, because the cycle can expire (dev override: 1 minute)
  between the check and `IPaymentDirectory.InitiateAsync`, and Payment has no cycle seam.

Recommended bounded change (needs Architect approval, exceeds this task's authorized Contracts
change):

- **R1 (preferred): make Payment enforce payability itself.** Extend the existing Order-implemented
  payability read `IPayableCheckoutReader.GetPayableAsync` / `PayableCheckoutSnapshot`
  (`Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs:32,42`) with the
  reservation-cycle deadline + state, and have `StorefrontPaymentOrchestrator.InitiateAsync` re-verify
  it immediately before recording the attempt. The invariant then holds inside the module that owns
  initiation, and no caller assertion is trusted. Then the Order endpoint becomes a thin
  "commit once → initiate once" composer over `IPaymentInitiationPort` (already delivered here).
- **R2 (fallback): single Order-owned transaction.** Expose a commit+payability primitive from the
  Order commit path that runs the cycle check inside the existing `TransactionScope`, returning a
  short-lived payability token that Payment must validate against the persisted cycle at initiation.
  Larger blast radius on Order; still requires the Payment-side validator, so R1 remains necessary.

Until one of these exists, publishing the endpoint would ship the unproven race the task explicitly
forbids; the Contracts seam alone is inert and safe to keep.
