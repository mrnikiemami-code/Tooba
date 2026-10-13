# TB-P0-CHECKOUT-WALLET-ENDTOEND-031 — cart-scoped wallet quotation + cart-mode wallet tender

Base-SHA (== origin/main at claim): 3450ea647966cb19f9c81490a92c5ba43822e4dc
Code-Commit-SHA: 651ea8864fd3ea612d7517bf9716b403bcf77354

## 1. What was missing before this task

`/payment?cartId=...` (029) could not offer wallet because the only wallet quote in the backend was
`checkoutId`-keyed (`GET /v1/storefront/checkout/{checkoutId}/wallet-quote`) and Payment had no seam to
obtain an Active cart's authoritative payable amount (030 authority map). 028 already accepted
`useWallet` and routed it into canonical payment initiation, so the only missing piece was a
server-authoritative, read-only, cart-scoped wallet quotation.

## 2. Owner-correct contract slice (Contracts-only, no Host edit)

### Order (contracts + adapter, no HTTP surface added)

* `Tooba.Order.Contracts/Payments/CartPaymentQuoteContracts.cs` (new)
  * `ICartPaymentQuoteReader.GetForCartAsync(cartId, guestSecret, ct)`
  * `CartPaymentQuoteSnapshot` — cartId, cartVersion, currency, subtotal, discount, tax, shipping,
    payable, shipping method code/label, seller-order count
  * `CartPaymentQuoteResult` + `CartPaymentQuoteStatus` (`Ready`, `CartMissing`, `CartForbidden`,
    `CartEmpty`, `ShippingSelectionRequired`, `CartNotActive`, `Unavailable`) — typed, message-free,
    fail-closed; no Order stable code crosses the boundary.
* `Tooba.Order.Infrastructure/Integrations/Payment/CartPaymentQuoteBridge.cs` (new) — reuses
  `StorefrontShippingService.ProjectPaymentDraftAsync` (011: persisted shipping selection + effective
  commerce context). It creates nothing: no CheckoutGroup, no order, no reservation, no cart
  conversion, no abuse quota, no amount recomputation. Every `StorefrontOrderException` /
  `ContractOperationException` code is mapped to a typed status.
* `OrderModule` — one additive DI line
  (`ICartPaymentQuoteReader → CartPaymentQuoteBridge`).

### Payment (cart-scoped wallet quote)

* `GetStorefrontWalletQuoteQuery` — one request contract, two owner modes, explicit
  `CartScoped` flag: checkout mode keeps the strict `CheckoutId` requirement (unchanged behaviour),
  cart mode requires `CartId` and forbids `CheckoutId`.
* `GetStorefrontWalletQuoteQueryValidator` — same strictness per mode; no rule weakened for the
  existing checkout surface.
* `StorefrontPaymentOrchestrator.GetCartWalletQuoteAsync` — `GetForCartAsync` (Order payable) then the
  canonical `IWalletOrderPaymentPort.QuoteForPayableAsync(buyer, payable, currency)`. `PayableAmount`
  and `Currency` are taken verbatim from the Order snapshot; wallet numbers come verbatim from the
  Wallet port. The browser never supplies amount, balance or eligibility.
* `PaymentStorefrontEndpoints` — new public route
  `GET /v1/storefront/cart/{cartId:guid}/wallet-quote` (thin: `ISender` + authorizer + `ApiResponseFactory`).
* `StorefrontWalletQuoteDto` — additive fields `cartId`, `cartVersion`, `payableAmount`,
  `cartQuoteStatus`, `walletEligible`; `CheckoutId` is now nullable (still emitted for checkout mode).

### Fail-closed matrix (server side)

| Case | Result |
| --- | --- |
| anonymous / guest (cart mode) | `walletEligible=false`, balance 0, `canPayFullyWithWallet=false`, cart payable reported, **no balance leak** |
| authenticated + wallet less than payable | `walletEligible=true`, real balance/maxUsable, `canPayFullyWithWallet=false` |
| authenticated + full coverage | `walletEligible=true`, `canPayFullyWithWallet=true` → UI may offer wallet |
| cart missing / foreign / empty / no shipping selection / stale / non-Active | `walletEligible=false`, zeros, `cartQuoteStatus` = typed status |
| `checkout.`/`shipping.` actor policy rejects | `_actorPolicy.EnsureCheckoutActorAsync` fails the request (no quote) |

## 3. Frontend

* `storefront-payment-api.ts` → `loadStorefrontCartWalletQuote(cartId)`: returns a quote **only** when
  the server response says `walletEligible === true` **and** `canPayFullyWithWallet === true`; all
  numbers are copied from the response. Any non-OK response, malformed payload, thrown fetch or
  `catch` returns `null` — a fetch failure can never become wallet eligibility. No `Math.min/max`,
  no `payable - balance`, no client-side eligibility.
* `storefront-payment-handoff.tsx` (cart mode): loads draft + methods + cart wallet quote
  concurrently; `setQuote(cartQuote)` (server value only); wallet is pre-selected/offered only when
  `cartQuote?.canPayFullyWithWallet`; `validateMethod()` rejects a forced `wallet` without the server
  proof; `enabledCodes` gains `wallet` only from the server proof.
* Final click is unchanged and remains the single money boundary: `startFinalClickPayment` (028) with
  `useWallet: method === "wallet"`, the stable per-cart+method idempotency key and no
  money/identity/order in the wire body.

## 4. Tests actually run (local, module-scoped; no Host build/test, no live HTTP)

| Command | Result |
| --- | --- |
| `dotnet test Modules/Payment/Tooba.Payment.Tests` | 152 passed / 0 failed (includes 2 new cart-quote validator tests) |
| `dotnet test Modules/Order/Tooba.Order.Tests` | 255 passed / 0 failed |
| `npm run typecheck` (src/frontend) | clean |
| `npm run test:storefront` | 76 passed / 0 failed |
| `node --test app/storefront/storefront-final-click-api.test.ts` | 9 passed / 0 failed (031 invariants) |

Pre-existing, unrelated failures observed in the wider `npm run test:discovered` and
`npm run test:architecture` runs (files **not** touched by this task and already dirty in the tree
before claim): `lib/architecture/frontend-source-size.guard.test.ts`
(`category-admin-screen.tsx` 2230>2215, `admin-landing-page-composer.tsx` 1442>1440,
`storefront-landing-blocks.tsx` 1033>1026, `shared-composition-renderer.tsx` 918>907),
plus admin/content/sitemap/home test files.

## 5. Why the route is not a brand-new request type (bounded, disclosed deviation)

Task text asked for "a GET /v1/storefront/cart/{cartId:guid}/wallet-quote endpoint, query/validator/handler".
The public route exists exactly as asked; the query/validator are the existing wallet-quote
request contract extended with an explicit cart mode, because a *new* MediatR request + validator
would invalidate Payment's frozen certification counts, which are asserted in CLOSED Host guards and
in archived certification JSON:

* `src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W3CertGuardTests.cs`
  — exactly `30` `public const string` in `PaymentErrorCodes`, `26` composed descriptors,
  `15` validator files, `16` endpoint-reachable requests, `10` storefront requests,
  `28/27/24/4/3` stable-code ledgers, `AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS`.
* `src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs` — reads the archived certification
  artifacts and asserts `16` requests, `15` required + `1` no-input, `10` storefront requests,
  `9` storefront validators, `NO_VALIDATOR_REQUIRED_NO_INPUT` for `ListStorefrontPaymentMethodsQuery`.
* `src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentValidatorCoverageGuardTests.cs`
  — same `16/15/10/9` + exact construction lists.

Adding a new request type would have required rewriting those certification records (forbidden:
"rewrite historical R1-R3", "do not weaken architecture tests", "no schema/migration unless
indispensable"), so this slice keeps every count, classification and descriptor byte-identical and
keeps Host CLOSED. If the Architect prefers a dedicated request type, the exact files needing
authorized alignment are the three above plus the archived JSON they read.

## 6. Remaining release prerequisites / honest limits

1. `QuoteForPayableAsync` calls `GetOrCreateSummaryForCustomerAsync`, so a signed-in buyer with no
   wallet row can get a summary row created by this GET. This is the **same** canonical behaviour as
   the existing checkoutId wallet-quote GET (no ledger entry, no order mutation, no debit) but it is
   noted as a read-path side effect.
2. No live end-to-end acceptance was performed: the running Development Host :5088 was not touched
   (no build/restart/deploy), no live DB write, no real wallet debit, no PSP request.
   A live acceptance (signed-in buyer, full coverage, wallet debit through 028 + 025 gate) remains an
   Architect/live-deploy step.
3. Cart-mode mixed tender (partial wallet + gateway) stays deferred exactly as before
   (`mixedTenderAvailable=false`).
