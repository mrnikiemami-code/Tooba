# TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167 — Evidence (verdict: PARTIAL)

Mode: `BOUNDED_IMPLEMENT_REAL_CANONICAL_FLAT_METHOD_CREATE`
Base: `333a9c9640d5be96ae5d697cf621043ad34e03e9`
Verdict: **PARTIAL** — real owner-level create contract, handler, endpoint, Admin form and focused
tests are implemented and green, but the scratch-only live HTTP create → GET → reopen proof was NOT
executed inside the timebox. No screenshot is claimed.

## Implemented (Fulfillment owner, no schema change)

| Artifact | Purpose |
| --- | --- |
| `Tooba.Fulfillment.Application/Shipping/Ports/IShippingOneLevelCatalogAdminCreate.cs` | Explicit create outcome statuses, create-name input, write seam, and the pure `ShippingOneLevelMethodBinding.Derive` rule (`service:option` ⇒ `(service, option)`, single segment ⇒ `(code, null)`). |
| `Tooba.Fulfillment.Application/Shipping/Commands/CreateOneLevelShippingMethodCommand.cs` | MediatR command + handler + created DTO. Validates the language registry before any write, deduplicates languages, maps every fail-closed status to a stable error code. |
| `Tooba.Fulfillment.Application/Shipping/Validators/CreateOneLevelShippingMethodCommandValidator.cs` | Transport shape (non-blank method code). |
| `Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogAdminCreate.cs` | Gate-first create: canonical code → derived binding → provider metadata shape → per-language names → duplicate code check → duplicate binding check → server sort order (`max+1`) → single `SaveChanges` (method row + translations). Always `is_active = false`. `UndefinedTable` ⇒ `CatalogNotReady`; PostgreSQL unique violations ⇒ `BindingDuplicate`/`ConcurrentConflict`. Writes **no** legacy service/option, rate, coverage, schedule, packaging or checkout/order data. |
| `Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs` | `POST /v1/admin/shipping-services/shipping-methods/one-level` behind `IFulfillmentAdminAuthorizer`. Body: `methodCode`, optional `providerKind`, `translations[{languageId,name}]`. No client binding/activation fields exist. |
| `Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs` | Registers the create seam. |
| `src/frontend/app/admin/shipping-services/flat-method-create-model.ts` | Pure form model: canonical-code shape, provider metadata shape, real-language name collection/limits, conservative response mapping, stable Persian failure messages. |
| `src/frontend/app/admin/shipping-services/flat-method-create-api.ts` | Authorized client for the create endpoint; 401/403 ⇒ `denied`, server failure ⇒ real error code, invalid payload ⇒ `error`; no optimistic success. |
| `src/frontend/app/admin/shipping-services/flat-screen.tsx` | Replaces the honest BLOCKED dialog with a real RTL form (code, optional provider metadata, one name input per server-registry language). Submitting is disabled while in flight; on success the flat list is re-read from the server and the new row is immediately View/Edit-able; row actions stay exactly View/Edit/Delete and the four non-nested tabs are unchanged. |

## Focused test evidence

Backend (in-memory provider; new file `Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelMethodCreateTests.cs`):

```
dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests \
  --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Shipping"
Passed! - Failed: 0, Passed: 155, Skipped: 0, Total: 155
```

Covers: disabled gate refuses before any DB access; malformed/duplicate code; legacy binding owned by
another row (`BindingDuplicate`) with a free code; non-canonical provider metadata; blank/over-long
name; duplicate language; unknown language rejected at the Application boundary; valid create persists
exactly one inactive row with real per-language names, server sort order and derived binding; optionless
code (`mahex`) ⇒ `(mahex, null)`; **no** `shipping_services`/`shipping_service_options` rows are created
(so the storefront tree cannot contain the method); the recognized methods and their bindings are
unchanged; handler maps every fail-closed outcome to a stable error code. The Fulfillment validator
coverage guard was updated to the new exact inventory (20 requests / 14 required validators / 8
Shipping validators) and passes.

Frontend:

```
node --experimental-strip-types --test \
  app/admin/shipping-services/flat-method-rows.test.ts \
  app/admin/shipping-services/shipping-one-level-tabs.test.ts \
  app/admin/shipping-services/shipping-one-level-method-name.test.ts \
  app/admin/shipping-services/flat-method-create-model.test.ts
tests 48, pass 48, fail 0

npx tsc --noEmit -p tsconfig.json
(no output — clean)
```

## Why PARTIAL instead of PASS

The task's PASS bar requires a real persisted create through the Admin entry point **plus** a runtime
create → GET → reopen proof. Within the hard timebox the code, endpoint, UI and focused tests were
completed and verified, but no isolated scratch Host/DB was booted and no authorized HTTP call was made.
Therefore:

* Not proven live: `POST …/shipping-methods/one-level` against PostgreSQL (unique indexes
  `ix_shipping_methods_method_code` / `ux_shipping_methods_legacy_binding`), immediate
  `GET …/one-level` read-back and reopening the created row in a real browser.
* Not proven live: that the new method is absent from the real storefront projection with
  `ShippingOwnedRateAuthority=Enabled` (proven only structurally in tests: no legacy catalog row is
  written, and the storefront tree reads only that catalog).
* No migration was added; none was needed (nullable option code + derived service code).

Production/feature flags were not changed in any committed file.

## Smallest next verification step

Reuse the TB-157/158/166 scratch harness: disposable PostgreSQL on a private port, Development Host on
a private port with `Tooba:ShippingOneLevelCatalog:Enabled=true`, one authorized admin session, then
`POST` (create `tipax:sameday` with fa/en names) → `GET one-level` (row present, inactive, correct
binding/translations) → browser reopen of the created row → `POST` the same code again (expect
`shipping_method.code.duplicate`, list unchanged) → storefront projection probe with the owned-rate
gate ON (new method absent/unquotable) → teardown.
