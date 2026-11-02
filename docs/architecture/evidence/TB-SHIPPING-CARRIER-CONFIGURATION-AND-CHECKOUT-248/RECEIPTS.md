# RECEIPTS — TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248

## Identity

- Task: `TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248`
- Parent: `TB-SHIPPING-CARRIER-ADAPTER-FOUNDATION-247`
- Channel: `tooba-main` / worker `tooba-worker-01`
- Base-SHA: `9d3f0ad296dec1d26dc25fb51a4b0d949ccf4f8b` (`origin/main` at claim)
- Official Tipax: `https://document.tipaxapi.org/#v4-docs` / `https://omtestapi.tipax.ir/swagger/v4/swagger.json`
- Pricing path: `POST /api/OM/v4/Pricing`

## Checks actually run

- `dotnet test Tooba.Shipping.Tests --filter FullyQualifiedName~Carriers|ShippingErrorCatalogAndResourcesTests|ShippingCapturedQuoteContractsEdgeTests|ShippingModuleStructureGuardTests|ShippingCapturedRevisionQuoteTests|TipaxV4` → Passed 48/48
- `dotnet test --filter FullyQualifiedName~ShippingPersistenceModelTests` → Passed 52/52
- `cd src/frontend && npm run typecheck` → 0
- `cd src/frontend && npm run build` → 0 (Next.js 15.5.23 compiled)

## Controlled contract (not live)

- Disabled/incomplete Tipax process-global options: zero HTTP (247 facts retained; account path uses secret store).
- Token cache: accountId + configurationVersion; invalidate on rotate.
- Captured Free: quoted 0 / 0 packaging.
- Captured CarrierApi without binding: `CarrierConfigurationIncomplete` / `carrier_binding_missing`; no money.
- Captured CarrierApi with bound stub provider: freight/packaging from provider; packaging kept separate.
- Unauthorized carrier: typed, no money, zero financial side effect.

## Live boundary

- No Tipax test username/password in checked-in appsettings. Section `Tooba:Shipping:Carriers:Tipax` remains fail-closed.
- No real provider HTTP. No connection-test success claimed.
- Canonical Host5088 health 200; FE3000 reachable (308 locale). Processes were not recycled onto this tree.
- Migration `20261102170000_CarrierConfiguration` authored, not applied to `tooba_alpha`.
- Draft `01a17d0d-e033-7000-af43-49a1daa28822` not mutated.

## Data / preservation

- No seed, reset, business SQL, screenshot, or user-cart/checkout mutation.
- Checked-in activation defaults remain OFF.
- R4 not implemented.
