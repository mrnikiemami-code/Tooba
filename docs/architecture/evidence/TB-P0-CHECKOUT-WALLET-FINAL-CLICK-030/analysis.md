# TB-P0-CHECKOUT-WALLET-FINAL-CLICK-030 — cart-mode wallet tender: authority map + bounded blocker

Base SHA: 0460533acfb8af2a70781f046ee7a086231e2bf3 (== origin/main at claim)
Scope attempted: close the cart-mode wallet gap disclosed by 029. Host CLOSED, no deployment.

## 1. Why wallet is currently disabled in cart mode (029)

`app/payment/storefront-payment-handoff.tsx:70-71` — the cart branch sets `quote = null`, because the only
wallet quote that exists is checkoutId-addressed. `StorefrontPaymentMethodPicker` renders the wallet row only
when `quote?.canPayFullyWithWallet` is true, so no wallet tender is offered and `validateMethod()` rejects a
forced `wallet` selection before any request (line 160). `useWallet` is derived solely from the selected
method (`storefront-final-click-api.ts`), so the wire cannot request wallet unless the server-side catalog
authorized it.

## 2. Owner-seam authority map (exact paths)

Existing Payment storefront routes — `src/backend/Modules/Payment/Tooba.Payment.Endpoints/Storefront/PaymentStorefrontEndpoints.cs:22-24`:
- `POST /v1/storefront/checkout/{checkoutId:guid}/payments`
- `GET  /v1/storefront/checkout/{checkoutId:guid}/wallet-quote`   ← wallet quote, **checkoutId-keyed only**
- `GET  /v1/storefront/payment-methods`                            ← store-scoped catalog (gateway/manual; no wallet row)

Wallet quote path:
- Query `GetStorefrontWalletQuoteQuery(CheckoutId, CartId, GuestSecret, AuthenticatedUserId)` —
  `Tooba.Payment.Application/Storefront/Queries/GetStorefrontWalletQuoteQuery.cs:6-9`
- Orchestrator `StorefrontPaymentOrchestrator.GetWalletQuoteAsync` — `.../Orchestration/StorefrontPaymentOrchestrator.cs:61-86`.
  It resolves payable through `_checkouts.GetForMutationAsync(checkoutId, cartId, guestSecret, userId)`; without a
  persisted checkout there is no payable amount, so this seam is unreachable for an Active cart.
- Wallet owner port `IWalletOrderPaymentPort.QuoteForPayableAsync(customerActorId, payableAmount, currency, ct)` —
  `Tooba.Wallet.Contracts/Payments/WalletOrderPaymentPort.cs:32` with `WalletCheckoutQuoteDto` at line 4.

Order → Payment owner seams (all **checkoutId-addressed**) — `Tooba.Order.Contracts/Payments/OrderPaymentContracts.cs:6-10`:
`ICheckoutPaymentAccessReader.GetForMutationAsync / GetOwnedForPaymentResultAsync / GetOwnedAsync`.
No cart-scoped payable/quote contract is exposed to Payment.

Payment project references already available (no new module coupling needed for the *consumer* side):
`Tooba.Payment.Application.csproj:15` (Wallet.Contracts) and `:16` (Order.Contracts).

Wallet customer read (actor-verified, no guest fallback): `GET /v1/customer/wallet` —
`Tooba.Wallet.Endpoints/Customer/WalletCustomerEndpoints.cs:26`, actor resolved by
`WalletCustomerAuthorizer.cs:13-36` (authenticated session or Development dev-actor header only).

Frontend: `loadStorefrontWalletQuote(checkoutId)` — `app/storefront/storefront-payment-api.ts:376`, calling
`/v1/storefront/checkout/{checkoutId}/wallet-quote?cartId=…` (line 383).

## 3. Finding

**There is no secure cart-scoped wallet eligibility endpoint, and Payment has no seam to obtain an Active
cart's payable amount.** Every existing wallet/Order read is keyed by a persisted `checkoutId`. Therefore a
correct pre-commit wallet quote cannot be assembled from existing contracts; it requires a new owner contract
plus a new read-only endpoint.

## 4. Decision (per task rule): BLOCK, no optimistic client enablement

The task's rule: if a correct cart-scoped quote would require more than one coherent bounded slice, do NOT add
optimistic client wallet enablement; report the exact missing owner contract and the minimum next slice, with
no unsafe public feature. That is the case here (new cross-module owner contract + new endpoint + validators +
DI + guard manifests + tests across two modules inside a 20-minute HARD box), so:

- wallet stays **disabled** in cart mode (029 behaviour preserved, no fake quote, no "always eligible" UI);
- the checkoutId-addressed wallet tender on the committed-order recovery path is unchanged;
- deliberately NOT done: client-side eligibility from `GET /v1/customer/wallet` + draft payable. It would
  compute the quote in the browser (browser-side amount/balance authority), diverge from Payment policy, and
  risk becoming a bypass around the registered fresh-money gate.

## 5. Minimum next slice (Architect-authorized, exact)

1. `Tooba.Order.Contracts`: add a read-only cart quote contract, e.g.
   `ICartPaymentQuoteReader.GetForCartAsync(cartId, guestSecret, authenticatedUserId, ct)` →
   `CartPaymentQuoteSnapshot(CartId, CartVersion, PayableAmount, Currency, ItemCount, ShippingMethodCode)`,
   implemented in Order.Infrastructure by reusing the existing 011 quote path (`StorefrontCheckoutService.PreviewAsync`
   with the persisted shipping selection; no order, no reservation, no conversion).
2. `Tooba.Payment.Application`: `GetStorefrontCartWalletQuoteQuery(cartId, guestSecret, authenticatedUserId)`
   + validator + handler → `IWalletOrderPaymentPort.QuoteForPayableAsync(actor, snapshot.PayableAmount, snapshot.Currency)`.
   Fail closed: no guest actor for wallet (account-scoped), unavailable/insufficient → wallet not offered.
3. `Tooba.Payment.Endpoints`: `GET /v1/storefront/cart/{cartId:guid}/wallet-quote` guarded by the existing
   `EnsureCheckoutActorAsync` policy (same as the committed quote).
4. Frontend cart mode: consume that contract, offer wallet only on canonical `canPayFullyWithWallet`, keep
   `useWallet=false` otherwise; final click stays 028 with the stable key — no money call before the click.
5. Guards/tests: Payment validator-coverage manifest entry, endpoint organization guard, and focused tests for
   unauthorized guest/actor, insufficient funds, full coverage, stale cart version, and zero money movement.

Residual race to keep explicit: a read-only pre-commit quote is point-in-time. The money authority stays the
028 server commit + the 025 fresh-payability gate, so a wallet attempt may still be rejected at Pay time and
must surface a recoverable persisted `checkoutId` (029 already does this).

## 6. Delivered in this task (bounded, non-money, no production-behaviour change)

Durable fail-closed guards in `app/storefront/storefront-final-click-api.test.ts`:
- cart mode must set `quote = null` and must never call the checkoutId-addressed wallet quote with a cart;
- cart mode must never auto-select or enable the wallet tender (catalog-driven tenders only);
- a forced wallet selection is rejected by validation before any request;
- `useWallet` is derived only from the server-authorized method.
