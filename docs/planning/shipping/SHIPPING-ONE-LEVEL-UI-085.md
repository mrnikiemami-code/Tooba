# SHIPPING-ONE-LEVEL-UI-085 — trusted admin read bridge + gated frontend consumer

Task: `TB-SHIPPING-ONE-LEVEL-TABS-085` (parent `TB-SHIPPING-ONE-LEVEL-TABS-084`)
Expected base SHA: `f3ce208e7df0c471d318f6e7a40fea9c173889bd`
Actual base SHA: `f3ce208e7df0c471d318f6e7a40fea9c173889bd` (exact, zero drift)
Mode: `BOUNDED_AUTHORIZED_ONE_LEVEL_ADMIN_READ_BRIDGE`

## 0. Preflight

`git fetch origin` → `HEAD == origin/main == f3ce208e7df0c471d318f6e7a40fea9c173889bd`, identical to the
expected base. No source drift, no `RECOVERY_CONFLICT`. Every pre-existing dirty/untracked path was
preserved (including the modified admin screens `admin-fulfillment-work-queue-screen.tsx` and
`admin-order-detail-screen.tsx` that predate this task); nothing was reset, stashed, rebased or
force-pushed.

Read before editing: `AGENTS.md`, `docs/ai/TOOBA-PIPELINE-PROTOCOL.md`, the 081–084 planning notes,
`ShippingOneLevelCatalogContracts.cs`, `ShippingOneLevelCatalogReader.cs`,
`ShippingOneLevelCatalogPolicy`, the 084 `page.tsx` / `shipping-one-level-gate.ts` /
`shipping-one-level-tabs.tsx`, the Fulfillment admin authorizer conventions
(`IFulfillmentAdminAuthorizer` / `FulfillmentAdminAuthorizer` / `IAdminPanelAccess`), the existing
`ShippingServiceEndpoints` and `ShippingMethodsEndpoints` patterns, and the Host composition file.

**Development one-level tables were NOT migrated.** The 083 migration exists in source only; the live
Development schema is unchanged. This task performed read-only inspection and did not migrate, seed or
backfill anything.

## 1. Mission A — trusted backend admin read bridge

The blocker reported by 084 was: no authenticated, read-only admin endpoint exposes the effective
`Tooba:ShippingOneLevelCatalog` gate, so the frontend could not truthfully enable or refuse the new shell.

Added (all module-owned, no Host business logic):

| file | role |
| --- | --- |
| `Tooba.Fulfillment.Application/Shipping/Ports/IShippingOneLevelCatalogAdminRead.cs` | read-only port + `ShippingOneLevelCatalogAdminSnapshot(Gate, CatalogReady, Methods)` |
| `Tooba.Fulfillment.Application/Shipping/Queries/ListOneLevelShippingMethodsQuery.cs` | MediatR query + handler returning `OneLevelShippingCatalogAdminDto(Gate, Ready, Methods)` |
| `Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogAdminRead.cs` | infrastructure implementation over `IOptions<ShippingOneLevelCatalogOptions>` + `IShippingOneLevelCatalogReader` |
| `Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs` | `GET /v1/admin/shipping-services/shipping-methods/one-level` (additive) |
| `Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs` | DI registration of the new port |

### Gate semantics (default OFF, fail-closed)

- The gate is evaluated **only** through `ShippingOneLevelCatalogPolicy.Evaluate(...)` over the
  authoritative server configuration. The endpoint never invents or infers gate state.
- Missing section ⇒ `Disabled`. Explicit `false` ⇒ `Disabled`. Only explicit `true` ⇒ `Enabled`.
- While the gate is `Disabled` **no database query runs at all** (`IShippingOneLevelCatalogReader` is not
  called) and the response is `gate=Disabled, ready=false, methods=[]`.
- This bridge reads and reports **only** the one-level catalog gate. It never reads, infers or mutates
  `Tooba:ShippingRateAuthority` or `Tooba:ShippingOperationalShadow`.

### Unmigrated table is a controlled NOT_READY, not a 500

When the gate is `Enabled` but the catalog table has not been migrated, PostgreSQL raises SQLSTATE `42P01`
(`undefined_table`). That single, known condition is mapped to `ready=false` (a controlled NOT_READY). Any
other database failure propagates untouched so it is never hidden. A disabled/unmigrated catalog is never
presented as a successful empty list.

### Single-level wire

`OneLevelShippingMethodListItemDto` carries only `MethodCode`, `Name`, `IsActive`, `SortOrder`. It has no
`providerKind`, no option count, no nested options and no legacy service/option binding, so the admin wire
cannot create a second merchant level. Inactive methods are retained and explicitly reported as inactive;
no label is invented when a translation is missing. `ShippingOneLevelCatalogReader` remains the only
reader; the legacy `shipping_services` / `shipping_service_options` endpoints and their nested options are
byte-unchanged.

## 2. Mission B — frontend trusted consumer

`app/admin/shipping-services/page.tsx` became a **Next.js Server Component** (it was a client component
with a dormant seam):

- it reads the admin bridge server-side via `buildUpstreamAuthHeaders()` + `Accept-Language`, with
  `cache: "no-store"`;
- bearer/session credentials stay on the server and are never rendered into HTML or logged (the page
  contains no `console.*` and no `"use client"`);
- `resolveOneLevelCatalogGateFromHost` is pure and fail-closed: only HTTP 200 with `gate === "Enabled"`
  **and** `ready === true` enables the new shell; a non-200, an unparseable body, `Disabled`, or
  `ready=false` all fall back to the untouched legacy `ShippingServicesScreen`;
- `mapHostOneLevelMethods` maps server rows defensively — a code that is not already canonical and ≤ 32
  characters is dropped rather than "normalized" into a new identity, duplicate rows are dropped, and a
  missing label stays missing;
- no query string, `localStorage`, `useSearchParams` or `URLSearchParams` participates in gate resolution,
  so there is no client bypass;
- tab 1 renders the server-provided rows directly (`initialMethods`); the absence of a client `loadMethods`
  is no longer treated as an error when the server already supplied data.

Tabs 2–5 remain honest placeholders naming their own wiring tasks. No write API is imported and no Save
control exists.

## 3. Safety verification

- `Tooba:ShippingOneLevelCatalog` is **still absent** from every appsettings file (grep-verified), so the
  effective gate remains `Disabled` by absence. The gate was not enabled.
- `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` have no appsettings section and were
  not touched; Checkout still uses the Legacy path.
- No schema change, no migration, no backfill, no seed, no DML/DDL on any live database.
- The Host was not restarted or reconfigured; no customer price, availability, COD settlement, capacity
  reservation or delivery promise was changed.
- The only source-file edits outside the new feature files are two test-manifest count updates and the
  additive endpoint registration; no unrelated admin file was modified.

## 4. Tests actually run

| command | result |
| --- | --- |
| `dotnet test Tooba.Fulfillment.Tests` (full module) | **pass 102, fail 0** |
| `dotnet test Tooba.Fulfillment.Tests --filter ShippingOneLevelAdminReadBridge\|FulfillmentValidatorCoverageGuard` | **pass 15, fail 0** |
| `node --experimental-strip-types --test app/admin/shipping-services/shipping-one-level-tabs.test.ts` | **pass 17, fail 0** |
| `node --experimental-strip-types --test app/admin/shipping-services/*.test.ts` | **pass 134, fail 0** |
| `npm run typecheck` | **clean, no errors** |
| `npm run test:architecture` | **pass 10, fail 1** — pre-existing `FE-SIZE` shrink-only finding, see below |
| `dotnet test Tooba.Host.Tests --filter FulfillmentModuleAmsc001W3CertGuard` | **BLOCKED** — `MSB3027`/`MSB3021` file locks held by the running `Tooba.Host (33548)` |

The `test:architecture` failure is the pre-existing `FE-SIZE` shrink-only guard reporting
`OVERSIZED_GROWTH` for `category-admin-screen.tsx` (2230 > 2215), `admin-landing-page-composer.tsx`
(1442 > 1440), `storefront-landing-blocks.tsx` (1033 > 1026), `storefront-shipping.tsx` (1033 > 945) and
`shared-composition-renderer.tsx` (918 > 907) — all untouched by this task and identical to the 084
finding. It is reported separately from any new regression.

The Host cert guard could not be executed because the Development Host process locks
`src/backend/Host/Tooba.Host/bin/Debug/net8.0/*.dll`. Restarting that Host is outside this task's
authorization, so the guard was not run. Its two affected assertions were verified by direct inspection:
module-owned route count went 21 → 22 (the single additive read route) and no module-owned route was
removed, and the module-owned endpoint files are unchanged apart from that one registration.

## 5. Remaining debt / blockers

1. The live one-level catalog is still unmigrated in Development and has no data (083), so even with the
   gate enabled the read bridge would honestly report `ready=false` until a backfill is separately
   authorized. The UI therefore still renders the legacy screen in the current environment.
2. Tab 1 is wired to real server rows, but tabs 2–5 remain shell destinations for 086+.
3. The Host AMSC cert guard could not be executed locally because the running Host holds its build output.

## 6. Rollback

Revert the additive backend files/registration and return `page.tsx` to render `ShippingServicesScreen`
unconditionally. No persisted state and no runtime configuration is involved, so rollback is a pure source
revert. The gate is OFF by absence, so behavioural rollback is already the current state.
