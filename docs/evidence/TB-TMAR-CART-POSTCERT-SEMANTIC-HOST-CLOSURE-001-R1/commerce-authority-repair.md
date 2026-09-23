# TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1 — Commerce Authority Repair

Task: `TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1`
Parent: `TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001`
Track: `CART_POSTCERT_COMMERCE_AUTHORITY_REPAIR`
Channel: `tooba-main`
Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`

## 1. Architect defect (reproduction)

Parent `-001` was PARTIALLY ACCEPTED; parent PASS was REJECTED for one remaining semantic defect.

`CartCommerceContextResolver` still resolved commerce authority inside Cart:

- Market from `ICurrentCommerceContext.Current.Tenant.DefaultMarketReference` with a Cart fallback.
- Currency from `CartCommerceDefaultsOptions.DefaultCurrency`.
- SalesChannel from `CartCommerceDefaultsOptions.DefaultSalesChannel`, whose code default was `SalesChannel.Direct`.

So Cart owned/invented `Market`, `Currency`, and `SalesChannel` defaults instead of consuming an
effective storefront/store commerce authority. A global Cart currency knob could collapse a
shared-DB deployment to one Cart currency, and a code-level channel literal acted as guest-cart
authority.

## 2. Authoritative sources inspected

| Boundary inspected | Finding |
| --- | --- |
| `Tooba.BuildingBlocks.CommerceContext` / `ICurrentCommerceContext` | Canonical per-request/worker commerce context. Previously carried `Edition`, `Tenant`, connection reference, and `TraceId` only — no effective currency or channel. `TenantContext.DefaultMarketReference` existed but is a market reference, not a currency or channel authority. |
| Tenant/store configuration (`ToobaPlatformOptions`, `TenantRecord`, `ControlPlaneRegistry`) | Platform control plane already owns Tenant + market reference and is the authoritative resolver boundary for request and worker paths. Currency and SalesChannel were absent. |
| `Tooba.Catalog.Contracts` | Catalog owns store settings (appearance, quantity, hold policy, checkout abuse). No market/currency/sales-channel store authority. |
| `Tooba.Pricing.Contracts` | `CurrencyCode` is a money-code value object with per-call validation; `PriceQuote` carries market/channel/currency of an authored price. Pricing does not own a store default currency policy. |
| `Tooba.Offer.Contracts` | `SalesChannel` is the canonical public enum; Offer owns channel per offer, not a store default. |
| Storefront/market settings contracts | No existing storefront commerce-authority contract supplying effective currency/channel. |
| `Host.Storefront.StorefrontComposer` | Reads market/currency from the resolved `PriceQuote`; it does not resolve an authoritative store currency. |
| `Host.Admin.*Settings*` | Store settings endpoints cover appearance/menu/hold/quantity; no commerce (market/currency/channel) authority. |

No existing authoritative boundary supplied effective `Currency` or `SalesChannel`. Per the task,
introducing a duplicate Cart default was forbidden, so the smallest correctly-owned seam was added
at the platform control-plane boundary (the owner of Tenant/market resolution), and Cart was changed
to consume it.

## 3. Final sources (after repair)

| Dimension | Final source | Owner |
| --- | --- | --- |
| Edition / Tenant / connection | `CommerceContext` (`Edition`, `Tenant`, `DatabaseConnectionReference`) via `ICurrentCommerceContext` | Platform / Host control plane |
| Market | `StoreCommerceContext.Market`, populated from explicit `StoreCommerce:Market`, else Tenant `DefaultMarketReference` (Single-Store) or deployment `Tooba:StoreCommerce:Market` (Marketplace) | Platform control plane |
| Currency | `StoreCommerceContext.Currency` from explicit `StoreCommerce:Currency` | Platform control plane |
| SalesChannel | `StoreCommerceContext.SalesChannel` (stable enum name) from explicit `StoreCommerce:SalesChannel` | Platform control plane |

Cart consumes the resolved `StoreCommerceContext`; it is not the authority and owns no default.

## 4. Removed Cart-owned defaults

- Deleted `Tooba.Cart.Application/Models/CartCommerceDefaultsOptions.cs` (the whole type, including
  `SectionName = "Cart:CommerceDefaults"`, `DefaultMarket`, `DefaultCurrency`, and the code-level
  `SalesChannel.Direct` default).
- `CartModule` no longer configures `CartCommerceDefaultsOptions` and no longer imports
  `Tooba.Cart.Application.Models`; the duplicate `using Tooba.Cart.Application;` was removed.
- `appsettings.json` no longer carries `Cart:CommerceDefaults`.
- `CartCommerceContextResolver` no longer takes `IOptions<CartCommerceDefaultsOptions>` and contains
  no channel/market/currency literal.

No default was replaced with `Marketplace` or any other hardcoded channel, and no global Cart
currency knob remains as effective store currency authority.

## 5. Platform-owned commerce authority introduced

Smallest correctly-owned seam at the platform control-plane boundary:

- `Tooba.BuildingBlocks.StoreCommerceContext(Market?, Currency?, SalesChannel?)` — a nullable,
  fail-closed carrier. Null/blank means "not resolved".
- `CommerceContext.StoreCommerce` — the optional effective storefront commerce context supplied by
  the platform for both request and worker paths (defaults to `null` for existing constructions).
- `ToobaPlatformOptions.StoreCommerce` + `TenantRecordOptions.StoreCommerce` — deployment-level
  (Marketplace) and per-tenant (Single-Store) configuration.
- `ControlPlaneRegistry.DeploymentStoreCommerce` and `TenantRecord.StoreCommerce` — resolved once at
  startup. Market falls back to the tenant/default market reference; currency and channel are only
  taken from explicit configuration (never invented).
- `TenantResolutionMiddleware` sets `StoreCommerce` on the request `CommerceContext`.
- `WorkerCommerceContextFactory` sets `StoreCommerce` for outbox and poll-target worker contexts.
- `appsettings.Development.json`: `store-alpha` gains `StoreCommerce { Currency: IRR, SalesChannel: Marketplace }`.

## 6. Per-store / shared-DB behavior

Because `StoreCommerce` is resolved per Tenant record and attached to that request's commerce
context, different tenants/stores on a shared database resolve different effective currencies and
channels. There is no single global Cart currency. In Marketplace edition the deployment-level
`Tooba:StoreCommerce` supplies the value and `Tenant` remains null, preserving compatibility.

## 7. Fail-closed behavior

`CartCommerceContextResolver` fails closed with stable semantic codes, with no Cart default:

- `cart.commerce.context_unavailable` — platform context absent.
- `cart.commerce.market_unconfigured` — market not resolved.
- `cart.commerce.currency_unconfigured` — currency not resolved.
- `cart.commerce.channel_unconfigured` — channel absent/unparseable.

`CreateGuestCartHandler` still calls `commerceContext.Resolve()` and passes the result to
`CreateGuestAsync`; no raw HTTP value is trusted and no fallback default exists inside Cart.

## 8. Durable guard added

`src/backend/Host/Tooba.Host.Tests/Architecture/HostCartResidualGuardTests.cs`:

- `CreateGuestCart_uses_commerce_context_without_hardcoded_values` now also rejects
  `SalesChannel.Direct` in the handler.
- New `Cart_owns_no_commerce_policy_default_and_consumes_platform_authority` verifies:
  - no `CartCommerceDefaultsOptions` type reference anywhere in Cart production;
  - no `SalesChannel.Direct` / `SalesChannel.Marketplace` literal in Cart production;
  - no `Cart:CommerceDefaults` section;
  - no `DefaultCurrency` / `DefaultMarket` / `DefaultSalesChannel` knob in Cart production;
  - the resolver consumes `StoreCommerce` via `ICurrentCommerceContext`, owns no `IOptions<>`, and
    emits the fail-closed `cart.commerce.*` codes;
  - Cart still has zero `Tooba.Host` dependency.
- `Cart_owns_expiry_worker_and_its_worker_seams` additionally asserts the Cart module registers no
  commerce defaults.

Structural assertions are preferred and used for the ownership claims.

## 9. Prior Host closure preserved

- Host still owns zero Cart-specific implementation classes; no Cart file re-entered Host.
- Cart expiry worker/options and `CatalogCartPersistenceHoursResolver` remain Cart-owned.
- Persistence-hours path remains fully async.
- Cart has no `Tooba.Host` dependency.
- Order certification, Checkout `PAUSED_AT_SAFE_W5_CHECKPOINT`, and `frontendFrozen` are unchanged.
- No routes, DB schema, or migrations were touched.

## 10. Validation

- `dotnet build src/backend/Tooba.slnx` — Build succeeded, 0 errors.
- `HostCartResidualGuardTests` (preservation + new commerce-authority guard).
- `TmarDurableGuardTests` (SoT coherence with R1 next-task stamp).
- `Tooba.Cart.Tests` focused suite.
- No broad unrelated suites were run.

## 11. Recovery SoT

- Cart structural certification NOT revoked: still `COMPLETE_REFERENCE_PATTERN` and
  `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
- Order remains certified; Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen` stays true.
- This R1 recorded as the latest post-certification repair.
- `nextTask = USER_REVIEW_CART_POSTCERT_SEMANTIC_HOST_CLOSURE_R1`;
  `nextTaskGate = USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE` preserved.
