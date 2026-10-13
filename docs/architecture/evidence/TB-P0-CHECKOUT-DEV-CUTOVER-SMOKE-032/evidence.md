# TB-P0-CHECKOUT-DEV-CUTOVER-SMOKE-032 — final integration verification + cutover readiness

Base-SHA (== origin/main == HEAD at claim): a271e7efd5028694af69d80bf12d64582c984dab
Code-Commit-SHA: <filled by the worker result> (frontend integration fixes)
automaticNextImplementationTask: NONE

## 1. Scope actually executed

Source-level end-to-end verification of the three tender choices (online gateway / manual card
transfer / full wallet) through the cart-addressed final click, the wallet quotation, replay and
post-commit recovery. Two directly established frontend defects were fixed; nothing else was changed.
No backend file was modified in this task.

## 2. Wire verification (route ⇄ DTO ⇄ client, source-level)

| Layer | Source | Verified |
| --- | --- | --- |
| Shipping continue (no commit) | `src/frontend/app/storefront/storefront-shipping.tsx` → `saveShippingSelection` + `/payment?cartId=` | no `shipping/commit`, no `checkoutId` redirect |
| Cartesian draft read | `GET /v1/storefront/cart/{cartId:guid}/payment-draft` — `Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs:40` ⇄ `loadCartPaymentDraft` | read-only; `CheckoutId = null`; amounts backend-owned |
| Cart wallet quote (031) | `GET /v1/storefront/cart/{cartId:guid}/wallet-quote` — `Tooba.Payment.Endpoints/Storefront/PaymentStorefrontEndpoints.cs:24` ⇄ `loadStorefrontCartWalletQuote` | camelCase DTO fields match client reads: `walletEligible`, `canPayFullyWithWallet`, `walletBalance`, `maxUsable`, `remainingPayable`, `payableAmount`, `cartId`, `currency`, `manualCardToCardEnabled` |
| Payment methods | `GET /v1/storefront/payment-methods` ⇄ `loadStorefrontPaymentMethods` | gateway/manual catalog only (no fabricated wallet) |
| Final click (028) | `POST /v1/storefront/cart/{cartId:guid}/start-payment` — `StorefrontStartPaymentEndpoints.cs:39` ⇄ `startFinalClickPayment` | body keys exactly `[expectedCartVersion, idempotencyKey, useWallet, providerCode, couponCode]`; no amount/order/user/deadline/guest secret |
| Idempotency | `finalClickIdempotencyKey(cartId, providerCode)` scoped `${cartId}.${provider}` in `sessionStorage`; backend derives `start-payment:commit:` / `start-payment:payment:` namespaces from the same raw key | stable per cart+method; method switch is a distinct intent by design |
| Cart version | body `expectedCartVersion = page.cartVersion` from the 011 draft; backend re-validates in `StorefrontShippingService.CommitAsync` | concurrency conflict fails closed (`checkout.version.conflict`) |
| Coupon | `readStoredCouponCode()` sent on both the draft GET and the final click; server re-prices | client never sends a total |
| Actor / guest headers | client sends `X-Tooba-Guest-Secret` (canonical header) + same-origin session cookie; backend `ReadGuestSecret` header/cookie and `IPaymentStorefrontAuthorizer` / `IOrderStorefrontActor` both resolve identity from `CurrentAuthenticatedSession` | the wallet quote buyer and the final-click debit buyer are the same session identity (no split identity between quote and debit) |
| Proxy / discovery | `src/frontend/next.config.ts` rewrites `/v1/:path*` → `TOOBA_HOST_ORIGIN` (default `http://127.0.0.1:5088`) | the new `/v1/storefront/cart/{id}/wallet-quote` is covered by the existing prefix rewrite; no config change needed |

## 3. Tender outcomes (source-level)

* **Wallet** — selectable only when the server proves `walletEligible === true` **and**
  `canPayFullyWithWallet === true`. Quote is read-only (011 evaluation + `QuoteForPayableAsync`), the
  debit happens only inside the 028 click, and Payment re-quotes at initiation and refuses
  (`payment.wallet.mixed_deferred`) when coverage moved: no debit, no fallback to gateway. Guests get
  `walletEligible=false` with no balance leak and cannot initiate a wallet debit.
* **Gateway** — selector `gateway` resolves before commit to the concrete `DefaultProvider` used by both
  the 018 hold policy and Payment initiation (one authority: `PaymentProviderSelectionAdapter`).
* **Manual** — selector `manual` resolves only when `ManualCardToCardEnabled`; otherwise the click fails
  closed with `payment.method.unavailable` before any commit/hold/payment.

## 4. Replay / post-commit recovery (verified, no change needed)

* Same key + recovered response → `ICheckoutProcessTracker.FindSucceededBySubmissionKeyAsync(commitKey)`
  replays the persisted checkout, re-checks ownership, and does **not** commit again
  (`StorefrontStartPaymentComposer` lines 249-266; covered by
  `StorefrontStartPaymentComposerTests.Lost_response_with_the_same_key_replays_the_checkout_without_a_second_commit`).
* Post-commit failure → ProblemDetails extension `checkoutId` (endpoint `Failure(...)`) → frontend
  recovery banner `/payment?checkoutId=...` on the same order, no second checkout.
* Wallet failure at initiation → typed code `payment.wallet.mixed_deferred`; frontend maps it to an
  actionable Persian message (fix B below) with no automatic retry/renewal.

## 5. Defects found and fixed in this task (2, frontend only)

**Fix A — cart-mode wallet quote hid the server's insufficiency evidence.**
`loadStorefrontCartWalletQuote` returned `null` whenever coverage was incomplete and forced
`canPayFullyWithWallet: true`, so the existing Payment UX notice `payment-wallet-insufficient`
(`storefront-payment-methods.tsx`) could never render in cart mode. It now returns the server quote
whenever the server proves the buyer is the wallet owner (`walletEligible === true`) and copies
`canPayFullyWithWallet` verbatim — wallet stays non-selectable and `validateMethod()` still refuses it
without server coverage. The session read also moved inside `try` so a storage failure cannot reject the
page-level `Promise.all` that also loads the draft and methods.

**Fix B — cart-mode wallet refusal was not localized.**
`toCustomerFinalClickMessage` had no wallet case, so a quote-change refusal
(`payment.wallet.mixed_deferred`) fell through to the generic branch. It now has explicit, actionable
cart-mode Persian messages for `payment.wallet.mixed_deferred` and `payment.wallet.insufficient`, with
no technical code or raw detail leakage.

## 6. Tests actually run (local, focused; no Host build/test, no live HTTP)

| Command | Result |
| --- | --- |
| `dotnet test Modules/Order/Tooba.Order.Tests` | Passed — Failed 0, Passed 255 |
| `dotnet test Modules/Payment/Tooba.Payment.Tests` | Passed — Failed 0, Passed 152 |
| `npm run typecheck` (src/frontend) | clean |
| `npm run test:storefront` | tests 76, pass 76, fail 0 |
| `node --test app/storefront/storefront-final-click-api.test.ts` | tests 10, pass 10, fail 0 (updated Fix A invariant + new Fix B regression) |

Pre-existing unrelated failures (untouched files, already dirty before claim):
`lib/architecture/frontend-source-size.guard.test.ts` (category-admin-screen.tsx 2230>2215,
admin-landing-page-composer.tsx 1442>1440, storefront-landing-blocks.tsx 1033>1026,
shared-composition-renderer.tsx 918>907) and admin/content/sitemap/home suites.

## 7. Development deployment truth (read-only inspection; nothing touched)

| Fact | Value |
| --- | --- |
| Running Host | PID 39992, `src/backend/Host/Tooba.Host/bin/Debug/net8.0/Tooba.Host.exe`, listening :5088 |
| Running binary version | `1.0.0+207dae32fd899584d8057405901593aecf222e45`, built 2026-10-13T01:44:47Z |
| Deployed commit | `207dae32` (2026-10-12T20:25:52+03:30) — **49 commits behind** current HEAD, ancestor |
| Frontend dev | node on :3000 (`next dev`), rewrites `/v1/:path*` → `http://127.0.0.1:5088` |
| Dependencies | `postgres-db` (postgres:17, :5432) and `rabbitmq` (:5672) already up 2 days |
| Schema/migration need | none in 029-032 (code + routes + one additive DI line only) |
| Bridge | :17321 (`CursorChatBridge`) |

The running Host predates the whole 010-032 checkout program, so the new routes
(`cart/{cartId}/payment-draft`, `cart/{cartId}/start-payment`, `cart/{cartId}/wallet-quote`) and the
wallet tender are **not** live until the cutover below is authorized.

## 8. READY_FOR_CUTOVER (not executed — requires explicit user authorization)

Reversible, same-environment, no new environment/port/DB:

1. Confirm authorization and a quiescent browser window (no in-flight checkout).
2. Rollback anchor: current binary already exists at
   `src/backend/Host/Tooba.Host/bin/Debug/net8.0/` (commit `207dae32`); back up that folder (or rely on
   `git checkout 207dae32` + rebuild) so the previous binary can be restored verbatim.
3. `git fetch && git checkout main && git pull` — expect HEAD == origin/main == the SHA returned in the
   worker result; stop if it drifted.
4. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug` (stop on any error).
5. Stop PID 39992 (only that process; leave postgres/rabbitmq/frontend up), then start
   `Tooba.Host.exe` from the same output folder with `ASPNETCORE_ENVIRONMENT=Development` on :5088.
6. Smoke (read-only first): `GET /v1/storefront/payment-methods` → 200; an authenticated
   `GET /v1/storefront/cart/{cartId}/wallet-quote` → 200 with `walletEligible` and
   `canPayFullyWithWallet`; a guest call → `walletEligible=false` and no balance.
7. End-to-end click smoke (signed-in buyer, full wallet coverage): cart → shipping continue (cart stays
   Active, no order yet) → `/payment?cartId=…` → wallet row visible → Pay → one order + one payment,
   wallet debited once; repeat the same click with the same key → same checkout/payment, no second
   debit. Then gateway and manual variants.
8. Rollback: stop the new process, restore/rebuild `207dae32`, start it on :5088. No DB restore is
   needed (no schema change); any smoke order created in step 7 is normal business data.

No step above was executed in this task, and no runtime PASS is claimed.

## 9. Residual risks / honestly untested

1. No live HTTP, no browser, no real wallet debit, no PSP request in this task (Host and DB untouched
   by instruction).
2. Wallet replay across a *method switch* (gateway → wallet) is not a same-key replay; the frontend
   clears the cart session after a successful click, so a switch is only reachable after a failed
   attempt and then fails closed on the no-longer-Active cart rather than double-charging.
3. `QuoteForPayableAsync` may create a wallet summary row for a signed-in buyer with none (same
   canonical behaviour as the existing checkoutId wallet quote; no ledger entry, no debit).
4. Behaviours 5-8 above are the acceptance that only the authorized live run can prove.
