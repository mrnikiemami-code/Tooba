# TB-SHIPPING-ONE-LEVEL-TABS-085 — evidence

| item | value |
| --- | --- |
| Task | `TB-SHIPPING-ONE-LEVEL-TABS-085` |
| Parent | `TB-SHIPPING-ONE-LEVEL-TABS-084` |
| Program | `TOOBA-SHIPPING-ONE-LEVEL-TABS` |
| Mode | `BOUNDED_AUTHORIZED_ONE_LEVEL_ADMIN_READ_BRIDGE` |
| Bridge task row | `7d1c2784-d339-43f6-ba46-871655f09d10` |
| Expected base SHA | `f3ce208e7df0c471d318f6e7a40fea9c173889bd` |
| Claimed at (UTC) | 2026-10-15T12:24:42Z |
| Stop state | `STOP_FOR_ARCHITECT` |
| Live database | **UNMIGRATED** — no schema/seed/DML was executed |

## Status

`PASS` for Mission A (real, authenticated, read-only backend admin read bridge, default OFF) and Mission B
(the gated server-side frontend consumer). One verification step is honestly reported as blocked: the Host
`FulfillmentModuleAmsc001W3CertGuard` test could not be executed because the running Development Host
(`Tooba.Host`, pid 33548) locks its own build output. See §4.

## Mission A — trusted backend admin read bridge

New / changed production paths:

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/Ports/IShippingOneLevelCatalogAdminRead.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/Queries/ListOneLevelShippingMethodsQuery.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogAdminRead.cs` (new)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs` (additive route)
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs` (DI)

Route: `GET /v1/admin/shipping-services/shipping-methods/one-level`, guarded by
`IFulfillmentAdminAuthorizer` (the module's established admin convention; no new auth bypass, no
permission widening).

Behavior:

| input | result |
| --- | --- |
| gate section absent | `gate=Disabled, ready=false, methods=[]`, **no DB query** |
| gate explicitly `false` | `gate=Disabled, ready=false, methods=[]`, **no DB query** |
| gate explicitly `true`, table migrated | `gate=Enabled, ready=true, methods=[…]` (inactive retained) |
| gate explicitly `true`, table missing (`42P01`) | `gate=Enabled, ready=false, methods=[]` — controlled NOT_READY, not a 500 |
| gate explicitly `true`, other DB failure | exception propagates (never swallowed) |

Single-level projection: `MethodCode`, `Name`, `IsActive`, `SortOrder` only. No `providerKind`, no option
count, no nested options, no legacy binding. No write path exists in the bridge.

## Mission B — gated frontend consumer

- `src/frontend/app/admin/shipping-services/page.tsx` is now a Server Component that reads the bridge
  server-side (`buildUpstreamAuthHeaders()`, `Accept-Language`, `cache: "no-store"`) and resolves the gate
  with the pure, fail-closed `resolveOneLevelCatalogGateFromHost`.
- `shipping-one-level-gate.ts` gained `resolveOneLevelCatalogGateFromHost` and a defensive
  `mapHostOneLevelMethods` (non-canonical/over-long codes are dropped, not normalized; duplicates dropped;
  no label invented).
- `shipping-one-level-tabs.tsx` accepts server-provided `initialMethods` and no longer treats the absence
  of a client loader as an error when server data exists.

Enablement requires HTTP 200 **and** `gate === "Enabled"` **and** `ready === true`. Everything else —
non-200, malformed body, `Disabled`, `ready=false`, network error — renders the untouched legacy
`ShippingServicesScreen`. Credentials never reach HTML and are not logged.

## 3. Gate / configuration proof (read-only)

- `Tooba:ShippingOneLevelCatalog` appears in **no** appsettings file (grep over
  `src/backend/Host/Tooba.Host/appsettings*.json` → no matches) ⇒ the effective gate is `Disabled` by
  absence and was **not** enabled.
- `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` likewise have no appsettings section
  and were not modified.
- The live Development one-level tables remain absent: no migration, backfill, seed or DML was run.

## 4. Verification actually run

| command | result |
| --- | --- |
| `dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests` | `Passed! Failed: 0, Passed: 102` |
| `dotnet test …Fulfillment.Tests --filter ShippingOneLevelAdminReadBridge\|FulfillmentValidatorCoverageGuard` | `Passed! Failed: 0, Passed: 15` |
| `node --experimental-strip-types --test app/admin/shipping-services/shipping-one-level-tabs.test.ts` | tests 17, pass 17, fail 0 |
| `node --experimental-strip-types --test app/admin/shipping-services/*.test.ts` | tests 134, pass 134, fail 0 |
| `npm run typecheck` | clean, no errors |
| `npm run test:architecture` | tests 11, pass 10, fail 1 — **pre-existing** `FE-SIZE`, unrelated |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FulfillmentModuleAmsc001W3CertGuard` | **BLOCKED** (`MSB3027`/`MSB3021`, locked by `Tooba.Host (33548)`) |

Focused backend tests cover: gate absent ⇒ disabled with zero reader calls; explicit false ⇒ disabled with
zero reader calls; explicit true ⇒ enabled and inactive rows retained; unmigrated table ⇒ controlled
not-ready; unexpected DB error ⇒ not swallowed; DTO serialization contains no `providerKind`,
`legacyServiceCode`, `legacyOptionCode`, `optionCount` or `options`.

Frontend tests cover: fail-closed gate mapping for 401/0/200-`Disabled`/200-`ready=false`/malformed body;
defensive row mapping that rejects non-canonical and over-long codes, drops duplicates and never invents a
label; server-side-only gate resolution with no `useSearchParams`/`URLSearchParams`/`localStorage` and no
`console.*`/`"use client"` in `page.tsx`; no write API or pricing/authority reference in the page; read-only
shell with no second level.

### Pre-existing architecture failure (recorded separately)

`npm run test:architecture` fails only in `FE-SIZE` (oversized files are shrink-only) for
`category-admin-screen.tsx` (2230 > 2215), `admin-landing-page-composer.tsx` (1442 > 1440),
`storefront-landing-blocks.tsx` (1033 > 1026), `storefront-shipping.tsx` (1033 > 945) and
`shared-composition-renderer.tsx` (918 > 907). All five are untouched by this task and identical to the 084
finding, so this is not a new regression.

### Blocked Host cert guard

`Tooba.Host.Tests` could not be built/run because the running Development Host process holds
`src/backend/Host/Tooba.Host/bin/Debug/net8.0/*.dll`. Restarting the Host is outside this task's
authorization. Its two affected assertions were verified by direct inspection instead: module-owned route
count is now 22 (was 21) with exactly one additive read route and no removed route, and the two changed
count assertions in `FulfillmentValidatorCoverageGuardTests` match the real inventory (16 endpoint-reachable
requests = 10 required + 6 no-validator-required).

## 5. Live checks (real, non-fabricated)

- Frontend dev server already listening on `127.0.0.1:3000` was only navigated to (nothing restarted):
  `GET /admin/shipping-services` → HTTP 200, legacy screen present, new shell absent, no `role="tablist"`
  from the new shell. This is the expected OFF-gate behavior in the current environment.
- `GET http://127.0.0.1:5088/v1/admin/shipping-services/shipping-methods/one-level` → **404** from the
  *running* Host. This is expected and honest: the running Host binary predates this commit, and restarting
  or redeploying the Host is not authorized by this task. The route exists in source and is covered by
  focused tests; no live-endpoint success is claimed.

## 6. Rollback

1. Gate stays OFF ⇒ zero behavioural change today, no data touched.
2. Source rollback: revert the four additive backend files + the DI line + the endpoint registration, and
   restore `page.tsx` to render `ShippingServicesScreen` unconditionally.
3. No persisted state, no migration and no runtime configuration is involved.
