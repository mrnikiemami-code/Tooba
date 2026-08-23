# Tooba — TB-P03-GATE Evidence

Gate:

```text
TB-P03-GATE
```

Date:

```text
2026-08-23
```

Predecessor SHA:

```text
9616603f90e2c28ea17d04c88fe5db9b6db952b9
```

Recommendation:

```text
P03_GATE_PASS
```

This is Cursor evidence for Architect review. It is not Architect ACCEPT and does not close P03.

## Validation (run during this Gate)

| Check | Result |
| --- | --- |
| `dotnet restore` `src/backend/Tooba.slnx` | pass |
| `dotnet build` | pass, 0 warnings, 0 errors |
| `dotnet test` | pass, 122 passed, 0 failed, 0 skipped |
| PostgreSQL Testcontainers | exercised (module foundations, checkout, payment, promotion, MassTransit SQL Transport) |
| SpiceDB Testcontainers | exercised (`SpiceDbIntegrationTests`) |
| `npm ci` in `src/frontend` | pass |
| `npm run typecheck` | pass |
| `npm run lint` | pass (`next lint`; deprecation notice only) |
| `npm run build` | pass (Next.js 15) |
| `git diff --check` | pass (checked after evidence/SoT edits) |

Backend path in this repository is `src/backend/Tooba.slnx`. Frontend path is `src/frontend`.

## Commerce invariant matrix

| Invariant | Evidence |
| --- | --- |
| Catalog Product ≠ Seller Offer | Separate `Catalog` / `Offer` modules; Host composes both; architecture tests forbid foreign Infrastructure refs |
| Product ≠ Price | `CatalogProduct` has no Price; Pricing owns Money via OfferId (`PricingFoundationTests`, Catalog domain grep) |
| Offer ≠ Price | Offer domain comments and tests: listing does not own Price |
| Product ≠ Inventory / Offer ≠ Inventory | `OnHand` lives on `StockPosition`; Inventory tests assert Product/Offer types lack `OnHand` |
| Cart ≠ Order | Separate Cart/Order schemas; checkout converts cart then unique `CartId` on checkout group |
| Order ≠ Payment | Payment schema + `IPayableCheckoutReader`; Payment Infrastructure does not reference `OrderDbContext` |
| Order ≠ Fulfillment | No Fulfillment module; order snapshots only |
| Promotion ≠ Base Price | `IPromotionEvaluator` consumes priced exclusive amount; does not write Pricing rows |
| Tax separate from Pricing | Tax calculator runs after promotion on post-discount exclusive; `TAX_NO_APPLICABLE_RULE` / `TAX_CALCULATION_ERROR` fail closed |

Forbidden fields `Product.Price`, `Product.Stock`, `Product.SellerId`, `Offer.Price`, `Offer.Stock` are not modeled as commercial truth. Seller is Party/org reference, not User. Membership is not SpiceDB authorization.

## Catalog

- Product is descriptive; variant belongs to product; typed attributes; category/brand descriptive (`CatalogDomain`, catalog foundation tests).
- Publication is not purchasability (offer + price + stock still required).
- No seller/price/stock columns on catalog product.

## Offer / marketplace

- One variant, many seller offers (`Offer` + inventory per offer).
- Seller identity is Party/Organization id, not User.
- Single-store still uses Offer abstraction (modules remain composed for all editions).

## Pricing

- Pricing owns money; target is OfferId; Market ≠ Locale ≠ Currency; Tax jurisdiction is Tax module; channel explicit; effective dating and overlap guards in pricing tests.
- Authored price is tax-exclusive; FX display is not stored as authored price.

## Inventory

- OnHand / Reserved / Available; multi-location; offer-scoped stock; concurrency-safe reserve; last-unit oversell prevented; release/consume (`InventoryDirectory`, `InventoryFoundationTests`).

## Cart

- Lines target OfferId; authenticated vs anonymous high-entropy seam; quote is non-authoritative (`PRICE_CHANGED` at checkout); reservation required to convert; multi-seller cart; Cart ≠ Order.
- Conversion modes `RequestToReserve` and `OnlinePurchase` exist on checkout/order.
- No background cart reconciliation worker: **DEFERRED_NON_BLOCKING** because unique `CheckoutGroup.CartId` plus unique idempotency key make a second commercial checkout for the same cart impossible at persistence.

## Checkout / Order

- Cart converts to checkout group + seller orders + line snapshots.
- `BuyerPartyId` is distinct from `PlacedByUserId`.
- Pricing revalidated; promotion re-evaluated; tax calculated; historical snapshots on lines (price, tax, discount).
- Unique `IdempotencyKey` and unique `CartId` on checkout group (`OrderDbContext`).

## Promotion

Sequence in `CheckoutDirectory`: price quote → `IPromotionEvaluator.EvaluateAsync` → tax on `PostDiscountTaxExclusiveAmount` → line snapshot.

- Typed percentage/fixed; stacking Exclusive vs Stackable; coupon normalization; `PROMOTION_CHANGED` when quoted discount mismatches.
- `IPromotionRedemptionLedger` is deferred evaluation-only (no quota claim in this foundation).

## Tax

- Exclusive base; jurisdiction + effective-dated rules; `NoApplicableRule` and `CalculationError` do not become zero tax at checkout.
- Line tax snapshot is persisted independently of later rule edits.

## Payment

- Amount/currency from payable checkout snapshot; client cannot choose amount.
- Initiate ≠ success; provider verification required; no PAN/CVV types in Payment module.
- Durable path: payment success domain event → module Outbox → `payment.succeeded.v1` → MassTransit PostgreSQL SQL Transport → `OrderPaymentSucceededHandler` (Order-owned) → Order local transaction + durable inbox.
- Duplicate delivery is inbox-idempotent. Payment Infrastructure has no `OrderDbContext` project reference.

## Multi-seller flow

Catalog variant → multiple offers → independent inventory → one cart with mixed sellers → seller-scoped `SellerOrder` rows → payment allocations across seller order ids without collapsing checkout to one seller.

Settlement/payout is out of scope.

## Single-store integrity

Host still composes Catalog, Offer, Pricing, Inventory, Cart, Order, Tax, Promotion, Payment as separate modules. No Product.Price/Stock shortcut.

## Cross-module boundaries

- No global `ToobaDbContext` / `AppDbContext` (`ArchitectureBoundaryTests`).
- FKs are intra-schema only (catalog tree, cart lines, order seller/lines, payment attempts).
- Application contracts/gateways/events across modules; Domain/Application do not reference MassTransit/Authzed SDKs (existing architecture tests).
- `Order.Application` references `Promotion.Application` only, not Promotion Infrastructure.

## Tenant isolation

Modules resolve connection via `ICurrentCommerceContext` / `ToobaNpgsql.ResolveForContext`. Host `TenantResolutionMiddleware` owns host mapping. Unknown host fail-closed. Commerce modules do not parse Host. Marketplace vs single-store remains edition/tenant context, not collapsed schemas.

## Messaging / Outbox

Module-owned Outbox registrations remain. Host: MassTransit **8.5.10** + `MassTransit.SqlTransport.PostgreSQL` **8.5.10**. No RabbitMQ packages. Payment→Order path covered by payment foundation tests (inbox + projection).

## Package audit

| Package | Version |
| --- | --- |
| .NET TFM | net8.0 |
| EF Core | 8.0.11 |
| Npgsql | 8.0.7 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.11 |
| MassTransit | 8.5.10 |
| MassTransit.SqlTransport.PostgreSQL | 8.5.10 |
| Authzed.Net | 1.6.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol / Extensions.Hosting | 1.15.3 |
| OpenTelemetry.Instrumentation.AspNetCore/Http/Runtime | 1.12.0 |
| Next.js | ^15.1.6 (package.json) |
| React | ^19.0.0 |
| Tailwind CSS | ^3.4.17 |

Absent: MassTransit.RabbitMQ, RabbitMQ.Client, MassTransit 9, Redis authz cache, Stripe/PayPal SDKs (`Architecture` / payment / cache tests).

No package upgrades in this Gate.

## Persian documentation

- `Directory.Build.props`: `GenerateDocumentationFile`, CS1591 as error for non-test projects.
- Gate build: 0 warnings including CS1591.
- Generated EF exclusions remain narrow.

## Concern classification

| Item | Class |
| --- | --- |
| OTel exporter 1.15.3 vs instrumentation 1.12.0 | DEFERRED_NON_BLOCKING |
| `/__platform-*` diagnostics (Development/Testing) | DEFERRED_NON_BLOCKING |
| Config-backed tenant registry | DEFERRED_NON_BLOCKING |
| Npgsql / MassTransit NodaTime constraint | DEFERRED_NON_BLOCKING |
| SQL Transport admin vs runtime credential split | DEFERRED_NON_BLOCKING |
| Generic durable Inbox beyond payment→order inbox | DEFERRED_NON_BLOCKING |
| MassTransit delayed redelivery / scheduler | DEFERRED_NON_BLOCKING |
| T006 custom Outbox vs MassTransit EF Outbox | DEFERRED_NON_BLOCKING |
| Process-local cache until Redis | DEFERRED_NON_BLOCKING |
| Identity real OTP delivery provider | DEFERRED_NON_BLOCKING |
| Keycloak / OIDC | DEFERRED_NON_BLOCKING |
| WebAuthn / passkeys | DEFERRED_NON_BLOCKING |
| Rate-limit / anti-fraud product | DEFERRED_NON_BLOCKING |
| CONDITIONAL_PERMISSION caveats (deny-closed) | DEFERRED_NON_BLOCKING |
| Redis authorization cache | DEFERRED_NON_BLOCKING |
| Cart background conversion reconciliation worker | DEFERRED_NON_BLOCKING |
| Promotion usage/redemption quota ledger | DEFERRED_NON_BLOCKING |
| Real payment PSP | DEFERRED_NON_BLOCKING |
| Refund / capture / void | DEFERRED_NON_BLOCKING |
| Seller settlement / payout | DEFERRED_NON_BLOCKING |
| Fulfillment / shipment | DEFERRED_NON_BLOCKING |
| Returns / RMA | DEFERRED_NON_BLOCKING |
| Commercial UI | DEFERRED_NON_BLOCKING |

No BLOCKER. No REPAIR_BEFORE_NEXT_PHASE required for P03 coherence.

## Mandatory future UX sequence (preserved)

Deep Shopeiva Study → template reuse map → Design System → Professional Data Grid → workspace interaction patterns → serious UI → visual evidence → Architect visual ACCEPT.

Backend/module boundary ≠ UI boundary. Weak UI/UX remains a product failure. No commercial UI work in this Gate.

## SoT before Architect review

```text
Last Architect Accepted Task: TB-P03-T009
Current Gate: TB-P03-GATE
Current Phase: P03 — Commerce Core
Gate State: AWAITING_ARCHITECT_ACCEPT
P03 is NOT COMPLETE
```

Roadmap does not name a P04/next implementation phase. P00 capability rows (Content, SEO, Search, …) remain architecture COMPLETE from P00, not an authorized next execution phase.

## Final recommendation

```text
P03_GATE_PASS
```

Architect ACCEPT is still required before P03 is COMPLETE.
