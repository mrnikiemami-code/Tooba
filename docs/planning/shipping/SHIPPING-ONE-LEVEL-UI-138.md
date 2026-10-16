# SHIPPING ONE-LEVEL UI — TB-138 (Bounded isolated grouped checkout final-submit / payment acceptance)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-138` (parent `TB-SHIPPING-ONE-LEVEL-TABS-137`)
Mode: `BOUNDED_ISOLATED_GROUPED_CHECKOUT_FINAL_SUBMIT_PAYMENT_ACCEPTANCE`
Status: **PARTIAL** — `STOP_FOR_ARCHITECT`
Base SHA: `f6c07a056524a5313ececce061d489609d1ca4bf` · Code SHA: **NONE** (observation-only, no source edits)

## Why this task was observation-only

The architect asked for the *furthest safely provable* boundary of the grouped checkout final
submit. Nothing in that boundary is a code defect by itself, so no source file was changed and no
implementation is claimed. The result is a precise acceptance ledger with two unproven items.

## Mission A — safety gate: PASS

The final storefront action is `POST /v1/storefront/cart/{cartId}/start-payment` (Order 028) or
`POST /v1/storefront/checkout/{checkoutId}/payments`, both funnelled through the single
`IPaymentInitiationPort → PaymentInitiationAdapter → StorefrontPaymentOrchestrator` seam.

A pre-existing, explicit, Development-only no-charge provider exists and was the only payment
mechanism exercised:

- `FakePaymentGateway` (ProviderCode `fake`) is registered **only** in the non-Production branch of
  `PaymentModule.AddServices`; Production wires `WebhookPaymentGateway` (real PSP with
  `AddHttpClient`) or `FailClosedPaymentGateway`.
- `FakePaymentGateway` performs no HTTP and no charge — `InitiateAsync` returns an **internal**
  redirect `/payment/sandbox?ref=…`, `VerifyAsync` only returns a verdict.
- The simulator is doubly gated: `IsSandboxSimulatorEnabled() => !IsProduction() && Mode == "Sandbox"`,
  and both sandbox orchestrator methods throw `SandboxUnavailable` otherwise.
- Effective isolated configuration: `Payment:Gateway` `Mode=Sandbox`, `DefaultProvider=fake`, and every
  PSP field empty (`WebhookSigningSecret`, `InitiateBaseUrl`, `StatusQueryBaseUrl`,
  `StatusQueryApiKey`, `AllowedStatusQueryHosts=[]`). The overlay does not modify `Payment:*` at all.
- The customer surface used is the shipped `/payment/sandbox` page, which states
  «این صفحه بانک واقعی نیست» and drives the canonical `VerifyPaymentCommand` pipeline — no fake
  success-event injection, no direct DB write, no auth bypass.

## Mission B — one E2E acceptance

Isolated disposable runtime: scratch Postgres `tooba-tb138-pg` (`tooba_138_scratch` /
`tooba_138_messaging`, `127.0.0.1:55460`), Host `127.0.0.1:5407` (`isolated-138`,
`store-alpha`/`tenant-138-scratch`), frontend `127.0.0.1:3107`. Fail-closed isolation guard. Shared
`:5088` / `:3000` / `tooba_alpha` untouched.

### Proven

1. **Nothing committed before the final click.** After Continue (one click) exactly two cart-level
   draft groups with fee `380000.00` exist and `checkouts=0, seller_orders=0, order_groups=0,
   payments=0, allocations=0` — so the draft is read-only and materially separated from acceptance.
2. **Exactly one accepted Checkout, one SellerOrder, two accepted ShippingGroups** with
   `state=Confirmed` for one real seller, and **disjoint OrderLine membership** (group `…8c90` ← line
   `…70d5`, group `…bd8e` ← line `…70d9`). Group charge total `380000.00` for a single seller.
3. **Charge reconciled exactly once**: payment `3,977,000` = `SellerOrder 3,597,000` (goods 3,300,000
   + standard 9% tax 297,000) + `StoreShippingGroup 180,000` + `StoreShippingGroup 200,000`. The two
   shipping allocations carry **real stable `ShippingGroupId` targets**, not the legacy sentinel, and
   there is no duplicate allocation.
4. **States distinguished**: `PAYMENT INITIATED` (`Pending`) → `PAYMENT PAID` (`Succeeded`,
   `provider=fake`) → `ORDER ACCEPTED` (`SellerOrder PendingPayment` → `Paid`,
   `TB-20261016072346-01-7cf0bf`). The sandbox verification ran **once**; no second customer click and
   no accidental double payment (one payment row, one idempotency key).
5. **Zero real-world charge** and no premature seller shipping payout anywhere.

### Unproven — first blocker

`payment.succeeded.v1` was published (`attempts=1`, not dead-lettered) and **Order consumed it**
(`order.payment_inbox` = that event id → SellerOrder `Paid`), but the **Fulfillment module never
consumed it**: `fulfillment.payment_inbox=0`, `fulfillment.fulfillments=0`, `items=0`,
`ff_groups=0`, `shipments=0`. The Fulfillment module is demonstrably live in the same composition
(7 methods, 5 services, 4 options, 14 translations, 10 migrations) and registers
`IIntegrationEventHandler<PaymentSucceededIntegrationEvent> → FulfillmentPaymentSucceededHandler →
FulfillmentDirectory.CreateFromPaidSellerOrdersAsync → FulfillmentUnit.CreateFromPaidOrder`, so this
is a real gap at the **Order → Fulfillment integration-event delivery/registration boundary**, not a
"not reachable" case. Deliberately not patched inside the timebox.

### Unproven — second item

The isolated database has **no `settlement` schema**, so the Settlement module's
`ShippingGroupFeeAllocation` (today only `PendingAllocation` is permitted by design) could not be read
back. `PendingAllocation` was therefore neither exercised nor contradicted, and no settlement row was
fabricated.

### Also not claimed

Retry/idempotency was not re-proved at runtime (a second click was not allowed); the existing focused
tests remain the available proof and were not re-run inside the timebox. No FulfillmentUnit, shipment
booking, dispatch, settlement split, refund, production readiness or migration certification is claimed.
