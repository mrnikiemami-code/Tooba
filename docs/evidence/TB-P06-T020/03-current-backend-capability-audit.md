# 03 — Current backend capability audit (TB-P06-T020)

Audit date: 2026-08-27  
Scope: existing Tooba Host + module capability for Wave 2 surfaces (seller coupons UI, checkout coupon apply, seller reviews list/response, notifications).  
Method: inspect `src/backend/Modules/*`, Host endpoint maps, and frontend panel nav honesty. **No source changes beyond this evidence file.**

## Executive summary

| Area | Backend exists? | Seller can already… | Wave 2 recommendation |
|---|---|---|---|
| Promotion / Discount / Coupon | **Yes** — dedicated `Tooba.Promotion` module + evaluator; **no** seller/admin HTTP CRUD | Nothing via panel APIs; definitions only via application directory / tests / seed paths | **EXTEND** module + add seller/admin HTTP; do **not** CREATE a second coupon module |
| Checkout / Order promotion apply | **Yes** — Order evaluates promotions and snapshots discounts; storefront **does not pass** `CouponCode` | N/A (buyer path) | **EXTEND** storefront checkout contract + UI to pass coupon; keep Order evaluator |
| Pricing | **Yes** — authored base price only; explicitly not promotions | Seller edits Offer/price via seller panel (not coupons) | **DEFER** Pricing changes for coupons |
| Offer | **Yes** — listing identity; no coupon ownership | List/get/patch own offers | **DEFER** Offer schema for coupons (scope via `SellerPartyId` on Promotion) |
| Reviews | **Yes** — product reviews + admin moderation; **no** seller list/reply | Nothing on reviews | **EXTEND** Reviews for seller list (+ reply if in scope); do **not** CREATE parallel module |
| Notifications | **No** module / endpoints | N/A | **CREATE** later or **DEFER** (still Wave-1 deferred; no Host owner) |
| Admin moderation | Reviews **live**; Promotions **none** | N/A | Reviews: **EXTEND** as needed; Promotions admin: **EXTEND** when seller write lands |

---

## 1. Promotion / Discount / Coupon (`Tooba.Promotion`)

### Exists?

**Yes.** Full module under:

- `src/backend/Modules/Promotion/Tooba.Promotion.Domain/PromotionDomain.cs` — `PromotionDefinition`, status/stacking/discount kinds, coupon normalizer, eligibility
- `src/backend/Modules/Promotion/Tooba.Promotion.Application/PromotionContracts.cs` — `IPromotionDirectory`, `IPromotionEvaluator`, `IPromotionRedemptionLedger`, DTOs
- `src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/PromotionDirectory.cs` — Create / Activate / Change / Expire / Evaluate
- `src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Persistence/PromotionDbContext.cs` — schema `promotion`, table `promotions`
- `src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/PromotionModule.cs` — DI registration (also in Host `ToobaModuleComposition`)
- Foundation tests: `src/backend/Host/Tooba.Host.Tests/PromotionFoundationTests.cs`
- PROJECT-STATE: Promotion & Discount Foundation **COMPLETE** (TB-P03-T009)

Key domain facts already modeled:

- Optional `CouponCode` (normalized); null = auto-apply when eligible
- Scoping axes include `SellerPartyId`, Offer, variant, category, market, channel, currency, customer/org, min qty/subtotal
- Stackable vs Exclusive; percentage / fixed amount
- `DeferredPromotionRedemptionLedger` always returns redeemable (quota not enforced)

### HTTP endpoints?

**None dedicated.** Grep of Host `*Endpoints*.cs` shows no `/v1/seller/promotions`, `/v1/admin/promotions`, or coupon routes. Mutation is application-layer only (`IPromotionDirectory`), exercised by tests and composition (storefront listing / checkout orchestration).

### What seller can already do

- **Cannot** create, list, activate, or expire promotions via seller HTTP (`SellerPanelEndpoints` has dashboard/offers/orders/dev-contexts only — `src/backend/Host/Tooba.Host/Seller/SellerPanelEndpoints.cs`).
- Seller panel can manage **Offers** (list/get/patch), not coupons.

### Gaps for Wave 2 (seller coupons UI)

- No list/get/create/activate/expire seller APIs scoped by `SellerPartyId`
- No SpiceDB/guard matrix beyond open `OpenPromotionUseCaseGuard`
- No redemption ledger / usage caps
- No admin promotion moderation/approval surface
- Frontend `/vendor-panel/coupons` is honest unavailable shell (see §7)

### Recommendation

**EXTEND** `Tooba.Promotion` + Host seller (and optionally admin) endpoints.  
**Do not CREATE** a separate Coupon module.  
**DEFER** full redemption concurrency and campaign CRM.

---

## 2. Pricing (`Tooba.Pricing`)

### Exists?

**Yes.** Authored base price foundation:

- `src/backend/Modules/Pricing/Tooba.Pricing.Application/PricingContracts.cs` — `IPriceDirectory`, `IPriceLookupGateway`, `PriceQuote`
- `src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/PriceDirectory.cs`, `PricingModule.cs`, `Persistence/PricingDbContext.cs`

Contracts explicitly state promotions/tax/FX/UI are **out of Pricing**.

### Seller capability

Seller offer patch / product workspace may change listing/price flows; coupons are not Pricing writes.

### Gaps / recommendation

Wave 2 coupons should continue to evaluate **on top of** Pricing quotes (as Order already does).  
**DEFER** any Pricing schema change for discounts. **EXTEND** Promotion + Order/storefront only.

---

## 3. Offer (`Tooba.Offer`)

### Exists?

**Yes.** Listing identity (variant + seller + channel), not price/coupon:

- `src/backend/Modules/Offer/Tooba.Offer.Application/OfferContracts.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/OfferDirectory.cs`, `OfferModule.cs`

### Seller capability (live)

- `GET /v1/seller/offers`, `GET /v1/seller/offers/{offerId}`, `PATCH /v1/seller/offers/{offerId}` — `SellerPanelEndpoints.cs`

### Gaps / recommendation

Promotions already can bind `OfferId` / `SellerPartyId`.  
**DEFER** embedding coupon tables into Offer. **EXTEND** Promotion ownership APIs instead.

---

## 4. Order / Checkout promotion apply

### Exists?

**Yes at Order orchestration; partial at storefront HTTP.**

| Piece | Path | Behavior |
|---|---|---|
| Command field | `SubmitCheckoutCommand.CouponCode` in `OrderContracts.cs` | Optional coupon input |
| Quote lock | `QuotedDiscountAmount` | Rejects if discount drifted (`PROMOTION_CHANGED`) |
| Evaluation | `CheckoutDirectory.cs` (~lines 312–376) | Calls `IPromotionEvaluator.EvaluateAsync` per line with `command.CouponCode`; persists discount + promotion snapshots on `OrderLine` |
| Snapshots | `OrderDomain.cs` / migration `OrderPromotionSnapshots` | `PromotionIdSnapshot`, name, code, discount amounts |
| Storefront HTTP | `StorefrontEndpoints.cs` — `POST /v1/storefront/checkout/preview`, `POST /v1/storefront/checkout` | Shipping/cart only; **no coupon body field wired** |
| Storefront composer | `StorefrontCheckoutComposer.BuildCommand` | Passes `CouponCode: null`, `QuotedDiscountAmount: null` always |
| Listing display | `StorefrontComposer.cs` | Evaluates promotions with `CouponCode: null` for promotional card amounts |

### What buyer/seller can already do

- **Buyer:** checkout can receive **auto** (no-code) promotions if definitions are Active and eligible; coupon-gated promotions **cannot** be applied via live storefront API/UI.
- **Seller:** sees order line totals after submit (discount already baked into snapshots) but has no coupon management.

### Gaps for Wave 2 (checkout apply)

1. Accept `couponCode` on preview/submit request DTOs and pass through `BuildCommand`
2. Surface apply/reject reasons (`COUPON_NOT_APPLICABLE`, etc.) honestly
3. Enable cart/checkout UI currently disabled (see §7)
4. Optional: redemption ledger beyond stub

### Recommendation

**EXTEND** existing Order + Promotion evaluation path and storefront endpoints.  
**Do not CREATE** a parallel checkout discount engine.  
**DEFER** multi-coupon stacking UX beyond domain Exclusive/Stackable rules already implemented.

---

## 5. Reviews (`Tooba.Reviews`)

### Exists?

**Yes.** Product review module with moderation:

- Domain: `src/backend/Modules/Reviews/Tooba.Reviews.Domain/ProductReview.cs` — Pending / Published / Rejected; verified purchase snapshot
- Contracts: `src/backend/Modules/Reviews/Tooba.Reviews.Application/ReviewContracts.cs` — submit, published page, home featured, admin pending moderation
- Infrastructure: `ReviewDirectory.cs`, `ReviewsModule.cs`, `Persistence/ReviewsDbContext.cs`
- Host: `src/backend/Host/Tooba.Host/Reviews/ReviewEndpoints.cs`

### Endpoints (live)

| Method | Path | Audience |
|---|---|---|
| GET | `/v1/storefront/products/{slug}/reviews` | Public published |
| POST | `/v1/customer/reviews` | Customer submit → Pending |
| GET | `/v1/admin/reviews` | Admin pending queue |
| POST | `/v1/admin/reviews/{reviewId}/publish` | Admin |
| POST | `/v1/admin/reviews/{reviewId}/reject` | Admin |

**No** `/v1/seller/reviews*` routes.

### What seller can already do

- **Nothing** for product reviews (no list, filter by seller offers/products, or seller response entity/API).
- Admin moderation UI is live (`/admin/reviews`, `admin-api.ts` moderate helpers).

### Gaps for Wave 2 (seller reviews list/response)

- Seller-scoped list of reviews for products/offers they sell (join via Catalog/Offer ownership, not cross-DbContext leaks)
- Seller response / reply model + endpoints (domain has **no** response fields today)
- Optional notifications on new review (**depends on Notifications** — currently absent)

### Recommendation

**EXTEND** `Tooba.Reviews` + Host seller endpoints for list (and reply if Wave 2 requires it).  
**Do not CREATE** a second reviews module.  
Seller reply is a **domain extension**, not a greenfield product.

---

## 6. Notifications

### Exists?

**No** dedicated module under `src/backend/Modules/` (AddressBook … Wishlist; no Notification).  
No Host `/v1/*/notifications` endpoints found.  
Prior evidence (`docs/evidence/TB-P06-T018/08-notification-foundation.md`) explicitly **deferred** Host notification foundation in Wave 1.

### Gaps for Wave 2

- Persistence (recipient, type, body, read state, tenant)
- APIs + SpiceDB recipient isolation
- Panel inbox + preference storage

### Recommendation

**CREATE** a Notifications module when selected — there is nothing to extend.  
For TB-P06-T020 / early Wave 2 commercial gaps, prefer **DEFER** unless the task explicitly scopes a minimal Host inbox; do not fake badges or preference saves.

---

## 7. Frontend skim (seller coupons / reviews / notifications)

### Vendor panel (`src/frontend/app/vendor-panel/`)

| Route | File | Nav status | Backend binding |
|---|---|---|---|
| `/vendor-panel/coupons` | `coupons/page.tsx` → `VendorCapabilityShell` | **Deferred** — listed in `VENDOR_DEFERRED_NAV_HREFS` in `vendor-shell.tsx`; not in live menu | Honest unavailable (“این بخش فعلاً در دسترس نیست”) |
| `/vendor-panel/reviews` | `reviews/page.tsx` → `VendorCapabilityShell` | **Deferred** — same | Honest unavailable |
| Live nav today | dashboard, products, orders, stories, fulfillments, returns, analytics, wallet, settings | `live: true` only | Coupons/reviews intentionally deep-link only |

`seller-api.ts` has **no** coupon/review client helpers.

### Customer panel notifications

| Route | File | Nav status |
|---|---|---|
| `/customer-panel/notifications` | `notifications/page.tsx` → `CustomerCapabilityShell` | **Deferred** — `CUSTOMER_DEFERRED_NAV_HREFS` in `customer-panel-shell.tsx` |
| Settings prefs | `settings/page.tsx` (`customer-settings-notifications-unavailable`) | Honest unavailable section |

### Storefront coupon UX

- Cart: `storefront-cart.tsx` — `#cart-coupon` input/button **disabled**; copy states secure coupon engine not wired (UI honesty; auto promotions may still affect listing/checkout totals when eligible without a code).
- Checkout API types (`storefront-checkout-api.ts`) map `discountAmount` from Host but do **not** send a coupon code.

### Admin reviews

- Live: `/admin/reviews` + shell nav item (`admin-shell.tsx`, `live: true`).
- No admin promotions/coupons UI found in this skim.

---

## 8. Admin moderation matrix

| Capability | Status | Key paths |
|---|---|---|
| Review moderation | **Live** | Host `ReviewEndpoints.cs`; FE `/admin/reviews`, `admin-api.ts` |
| Promotion / coupon moderation | **Missing** | No admin promotion endpoints or screens |
| Notifications moderation/inbox | **Missing** | No module |

---

## 9. Wave 2 recommendation rollup

| Wave 2 ask | Recommendation | Rationale |
|---|---|---|
| Seller coupons UI | **EXTEND** Promotion + add `/v1/seller/promotions*` (and FE live nav when APIs exist) | Domain + evaluator already own coupons and `SellerPartyId` |
| Checkout apply coupon | **EXTEND** storefront checkout DTOs/composer + enable cart/checkout UI | Order already evaluates `CouponCode`; Host currently hard-nulls it |
| Seller reviews list | **EXTEND** Reviews + seller HTTP list | Product review store + admin queue exist; seller projection missing |
| Seller review response | **EXTEND** Reviews domain (new reply fields/APIs) | No reply model today; still not a new module |
| Notifications inbox/prefs | **DEFER** (or **CREATE** only if task scopes Host module) | Zero backend; Wave 1 already deferred honestly |
| Pricing/Offer redesign for discounts | **DEFER** | Boundaries already correct |

### Suggested implementation order (advisory only)

1. Storefront coupon pass-through on existing checkout evaluate path (unlocks buyer apply quickly).  
2. Seller Promotion CRUD/list scoped by seller party (unlocks coupons page).  
3. Seller Reviews list (read) → then reply if required.  
4. Notifications module only after commercial list/reply needs an inbox owner.

---

## 10. Explicit non-findings

- No `Tooba.Discount` / `Tooba.Coupon` separate module — coupon is a field on `PromotionDefinition`.
- No seller review response / Q&A-for-reviews entity in Reviews (ProductQnA is a different module: questions, not product star reviews).
- Story “review” workflow (`Story` module ownership/moderation) is **out of scope** for product Reviews Wave 2 surfaces above.
