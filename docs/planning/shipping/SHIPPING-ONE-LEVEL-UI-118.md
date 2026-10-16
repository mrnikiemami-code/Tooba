# SHIPPING-ONE-LEVEL-UI-118 — Read-Only Storefront Shipping One-Level Parity Audit

Parent task: `TB-SHIPPING-ONE-LEVEL-TABS-118` (Parent-Task `TB-SHIPPING-ONE-LEVEL-TABS-117`).
Mode: `BOUNDED_READ_ONLY_STOREFRONT_SHIPPING_ONE_LEVEL_PARITY_AUDIT` — observation only, no writes.
Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-118/**`.

## Result

`PARTIAL` — the source path is fully grounded and one representative client path is proven by a focused
test, but the **live** option/label mapping cannot be observed under the current shared configuration
because the projection endpoint is identity-gated. One minimal next proof is proposed.

## The three questions

### (a) Does the displayed one-level UI show the expected label/option mapping? — NOT OBSERVABLE LIVE (identity-gated) + NOT ENABLED

**Proven by direct read-only HTTP observation** (`storefront-probe.txt`, `rendered-surface-window.txt`):

- `GET http://127.0.0.1:3000/fa/shipping` → **200**, 110 620 bytes of SSR HTML.
- The one-level step surface **is** rendered: `data-testid="shipping-steps"` containing
  `shipping-step-1` (سبد خرید → `href="/fa/cart"`, red/inactive), `shipping-step-2` (ارسال, `bg-primary`
  = active), `shipping-step-3` (پرداخت, grey/inactive). Hero title «اطلاعات ارسال» and subtitle
  «آدرس و روش ارسال خود را انتخاب کنید» are present.
- The **option/label rows are absent from the server render**: `shipping-methods`, `shipping-method-post`,
  `shipping-delivery`, `shipping-summary` and `shipping-continue` all `absent`; the page renders the
  no-projection fallback «در حال آماده‌سازی ارسال…» + «بازگشت به سبد». (`روش ارسال` appears only inside
  the hero subtitle «آدرس و روش ارسال خود را انتخاب کنید», **not** as the methods-section heading.)
- The exact reason is the identity gate: `POST /v1/storefront/shipping/projection` without identity →
  **401** `checkout.authentication_required`.

So a real label/option mapping observation requires a customer identity + a real cartId — which this
task forbids synthesizing.

**Source-grounded mapping** (see `source-chain.md`): the rendered label is `projection.methods[].label`,
which comes from the Order-owned projection; `StorefrontShippingService.ApplyOneLevelDisplayNamesAsync`
(`:434-461`) may override **only** `Label` via the Fulfillment-owned seam
`ShippingOneLevelStorefrontLabelRead`. **That overlay is fail-closed OFF under the current shared
configuration** because `Tooba:ShippingOneLevelCatalog` is absent from every appsettings
(`one-level-flag-absence.txt`: 0 matches) → `ResolveDisplayNamesAsync` returns `Empty` at `:53-56` without
touching the database, so the storefront shows the **legacy** labels. The one-level catalog labels exist
and are readable only through the admin surface, which is also gated
(`GET /v1/admin/shipping-services/shipping-methods/one-level` → **401** `admin.actor.missing`).

### (b) Is a shipping option selected/retained only through its proper owner, and not as a side effect of navigating toward Payment? — YES (proven)

- **Focused test run once: 8/8 pass** (`focused-frontend-test.txt`), including
  *"shipping continue persists the draft, keeps the cart active and never commits"*, which asserts that
  `saveShippingSelection` occurs **before** `router.push('/payment?cartId=…')` and that the continue path
  contains no `shipping/commit`, no `checkoutId`, and no cart-clearing call.
- Source: `onSelectMethod` (`storefront-shipping.tsx:317-325`) changes only client state and re-reads the
  projection (a read-only POST). Persistence happens only on the explicit «ادامه به پرداخت» press via
  `PUT /v1/storefront/shipping/selection` into the Order-owned draft store.
- The legacy early-commit helper (`commitShippingToPayment`, `storefront-shipping-api.ts:383`) has **no
  caller anywhere in the frontend**; it survives only for explicit legacy recovery.
- Consequently, merely navigating toward Payment cannot create an order/reservation, and it cannot
  silently retain a shipping choice that the customer did not confirm.

### (c) Which exact gate/endpoint/state prevents a real non-mutating acceptance proof? — `checkout.authentication_required`

| Blocker | Exact evidence |
| --- | --- |
| Customer identity gate on the very endpoint that produces the labels | `POST /v1/storefront/shipping/projection` (no identity) → **401** `checkout.authentication_required`, thrown by `CheckoutIdentityGate.EnsureCheckoutActorAsync` (`CheckoutIdentityGate.cs:41-56`) from the Catalog policy `ICatalogCheckoutIdentityPolicyLookup` (default `AuthenticatedOnly`), invoked at `StorefrontOrderEndpoints.cs:152` |
| No bypass | repository `.cursor/rules/…` + task forbid synthesizing a session/cart/actor; a real proof needs an authenticated customer session (or a `GuestAllowed` policy) **and** a real `cartId` + guest secret |
| One-level label overlay is off | `Tooba:ShippingOneLevelCatalog` absent from appsettings → `ShippingOneLevelCatalogPolicy.Evaluate` returns `Disabled` → label seam returns `Empty` (no DB read) |
| Admin label source is gated too | `GET /v1/admin/shipping-services/shipping-methods/one-level` → **401** `admin.actor.missing` |

Route-mapping proof without invoking the write paths: `GET /v1/storefront/shipping/projection|selection|commit`
each → **405** (mapped, wrong verb).

## Proposed next minimal proof (separately authorizable)

ONE isolated Development Host launch (same guarded pattern as TB-117) seeded with a **GuestAllowed**
checkout identity policy, plus ONE storefront GET-render of `/fa/shipping` against that isolated Host with
a live scratch cart. That yields the real option/label rows and the real selection retention without
touching shared `tooba_alpha`, without creating orders, and without enabling any shipping feature flag.
If the Architect instead wants the **one-level catalog labels** specifically, the same isolated launch
must additionally set `Tooba:ShippingOneLevelCatalog:Enabled=true` — a flag toggle that is explicitly out
of scope here.

## Shared parity / non-interference

- Shared `:5088` PID 35624 `/health` 200; `:3000` PID 31580 → 308 `/fa`; `/__platform-commerce`
  unchanged (`dev-singlestore` / `store-alpha` / `tenant-alpha` / `theme-alpha` / `IR`).
- `tooba_alpha` counts byte-identical to the TB-117 baseline: methods 7, translations 14, carts 50,
  cart_lines 78, drafts 14, seller_orders 26, products 567, offers 3223 (`shared-postflight-counts.txt`).
- No cart/order created, no shipping option selected, no feature gate toggled, no PUT/POST-with-side-effect
  issued. The single identity-less `POST /v1/storefront/shipping/projection` was rejected by the gate before
  any handler ran; the gate is a pure read of the Catalog policy.

## Boundaries and non-claims

This is a *presentation/data-flow* audit only. It does not certify storefront labels, cart reachability,
checkout cutover, option parity at the storefront, or commercial shipping readiness. A 200 `/health`
and populated scratch templates are not Shipping acceptance.

## Rollback

Docs/evidence only — nothing to roll back.

## Stop state

`STOP_FOR_ARCHITECT`; `automaticNextImplementationTask = NONE`.
