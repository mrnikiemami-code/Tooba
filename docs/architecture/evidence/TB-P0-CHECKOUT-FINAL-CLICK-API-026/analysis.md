# TB-P0-CHECKOUT-FINAL-CLICK-API-026 — BLOCKED analysis (no production change)

Scope read: Order storefront endpoints, the accepted 019 composer, 018 hold preparation, Payment selector→provider
resolution, 025 gate. **Zero production files and zero test/guard files changed.**

## Verdict

`BLOCKED_ARCHITECT_DECISION`. Publishing `POST /v1/storefront/cart/{cartId:guid}/start-payment` is **not safe within
this bounded task**, for two independent reasons: (B1) an accepted durable architecture guard explicitly forbids any
route/node referencing the composer under `Tooba.Order.Endpoints`, and (B2) the composer's hold deadline is derived
from the method **selector** while Payment initiates with the **concrete** provider, so the prepared reservation hold
can be undersized versus the selected provider's own policy — the exact condition the task told us to stop on.

## B1 — a locked 019 guard forbids the required route location

`src/backend/Modules/Order/Tooba.Order.Tests/Architecture/StorefrontStartPaymentComposerGuardTests.cs`, added by the
accepted task 019 (commit `fefc4f428`), contains:

```text
No_module_or_host_route_publishes_start_payment():
  foreach root in [ Tooba.Order.Endpoints, Host/Tooba.Host ]:
    foreach file in *.cs:
      Assert.DoesNotContain("start-payment", text)
      Assert.DoesNotContain("StorefrontStartPaymentComposer", text)
```

The mission's own path literal contains `start-payment`, and any handler must reference the composer type. So the
required field is refused **by string** anywhere under `Tooba.Order.Endpoints` (and under Host). The guard is tracked,
accepted, and part of the module's certified architecture surface — i.e. locked governance. Relaxing it is a durable
guard weakening, which this task does not authorize (it only permits "focused Order module tests and endpoint
route/source guards"), and it is exactly the kind of change that requires explicit Architect ACCEPT.

Secondary, same class: `OrderStorefrontArchitectureGuardTests.Order_endpoints_own_nine_storefront_routes_via_ISender`
certifies that file as the **ISender-only** storefront surface. The final-click route is service-based (not MediatR),
so placing it there would contradict that guard's certified semantics even after B1 is relaxed.

Note: the composer's own guard `Composer_publishes_no_http_endpoint_and_stays_inert` and
`Order_module_registers_the_inert_composer` remain satisfiable — the blocker is the *route/publishing* guard, not the
composer itself.

## B2 — selector vs concrete provider makes the hold potentially undersized

Chain, with evidence:

1. Composer 019 builds the 018 request with the **selector**:
   `StorefrontStartPaymentComposer.cs:223-226` → `methodSelector = request.UseWallet ? Wallet : request.ProviderCode?.Trim() ?? ""`,
   passed as `OrderPaymentHoldPreparationRequest(..., methodSelector)` at `:283-289`.
2. 018 resolves the reservation deadline from that string only:
   `OrderPaymentHoldPreparationService.cs:153` → `_holdPolicy.ResolveUnpaidTimeoutAt(providerCode, now)`, with
   `providerCode = request.ProviderCode?.Trim()` at `:87`.
3. Policy lookup is keyed by that string: `CommerceHoldPolicySource.cs:92-99`
   `FindMethod(providerCode)` → `_methods.TryGetValue(providerCode.Trim(), ...)`, where `_methods` is
   `PaymentMethodHoldOverrideDto` rows keyed by `x.ProviderCode` (`:87-88`).
4. Payment resolves the **concrete** provider for the catalog alias:
   `StorefrontPaymentOrchestrator.cs:124` and `:141-153` → for `PaymentProviderCodes.GatewayCatalog == "gateway"`
   (`PaymentProviderCodes.cs:8`) it replaces the selector with `_catalog.DefaultProvider`
   (`PaymentGatewayCatalogAdapter.cs:23`, default `"fake"` per `PaymentGatewayOptions.cs:21`).
5. Payment assigns its unpaid timeout from the **concrete** code:
   `PaymentUnpaidTimeoutAssigner.cs:20` → `holdPolicy.ResolveUnpaidTimeoutAt(payment.ProviderCode, now)`.

Consequence: with the default-gateway selection, 018 prepares the reservation hold under key `"gateway"` while
Payment's own unpaid timeout uses the concrete provider key (e.g. `"fake"`). A hold override configured for the
concrete provider therefore applies to Payment but **not** to the 018 reservation hold, so the reservation hold can be
shorter than the selected provider's policy. The composer's own doc comment already admits this
(`StorefrontStartPaymentComposer.cs` XML remarks on the deadline being "store/platform layer and not the final
provider override"). Task 026 explicitly says: "do not allow initiation with an undersized hold versus selected
provider's policy. If unsafe, STOP with pinpoint evidence rather than enabling an endpoint."

## Precise minimal repair (needs Architect ACCEPT / decision)

1. **Route guard**: narrow `StorefrontStartPaymentComposerGuardTests.No_module_or_host_route_publishes_start_payment`
   to allow exactly one approved route constant + one handler, while keeping the composer itself endpoint-free and
   keeping the "no other module/Host route publishes start-payment" assertion. Decide whether the route lives in
   `StorefrontOrderEndpoints.cs` (ISender-only, would also retire that guard's certification statement) or in a new
   dedicated file under `Tooba.Order.Endpoints/Storefront` (also needs the guard relaxed, plus an
   `OrderEndpointOrganizationGuardTests` entry).
2. **Method-policy authority** (choose one, all require a decision because they touch 018/025 safety):
   (a) the new route resolves the selector to the concrete provider through an Order.Contracts port before calling
   018, so 018 and Payment use the same key; or (b) the final-click route refuses the `"gateway"` alias and requires
   an explicit concrete provider code; or (c) Payment exposes the effective provider policy for the selector and 018
   consumes it. No option is implementable inside this task's file budget without also revisiting 018's contract.
3. **Error mapping**: `InvalidRequest`/`IdempotencyKeyRequired`/`IdempotencyKeyTooLong` need an existing registered
   4xx code decision; `HoldPreparationFailed` must not forge a Payment code, and `CheckoutCommitFailed` already
   carries the owner's typed code. No new code was invented here.

## What was deliberately NOT done

No endpoint, no route literal, no composer wiring, no guard relaxation, no error-catalog or localization change, no
DI change, no Host edit. Docs-only.
