# SHIPPING-ONE-LEVEL-UI-086 — tab 2 «تعرفه و پوشش»

Task: `TB-SHIPPING-ONE-LEVEL-TABS-086` (parent `TB-SHIPPING-ONE-LEVEL-TABS-085`)
Mode: `BOUNDED_ONE_LEVEL_RATES_AND_COVERAGE_TAB`
Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `a0c320953b83f6daf4547c8fa9f3d4223274f825`

## 1. Scope actually delivered

Tab 2 of the default-OFF one-level shell is now a real, working rates-and-coverage surface for
**exactly one** selected one-level method. It reuses the existing Shipping-owned admin rate and
coverage APIs, the existing tier-condition model, and the existing Geography destination picker.
It does not add a second merchant level, does not touch customer Checkout, does not migrate or seed
any data, and leaves the legacy two-level screen (gate OFF) fully operational.

## 2. The identity problem and the chosen resolution (Mission A)

A one-level method is identified by its canonical persisted `method_code` (≤32 chars). Its hidden
legacy binding (`legacy_service_code` + nullable `legacy_option_code`) is required to reuse the
existing Shipping rate/coverage APIs, which are addressed by service code plus optional option code.

Splitting `method_code` in the browser is unsafe and was explicitly forbidden. The 085 admin DTO
deliberately omits the binding. The narrowest authorized resolution was therefore added server-side:

| Layer | Artifact | Purpose |
| --- | --- | --- |
| Contracts | *(unchanged)* | The 085 public list DTO still excludes the internal binding. |
| Application port | `IShippingOneLevelCatalogAdminRead.ResolveBindingAsync` + `ShippingOneLevelCatalogAdminBindingSnapshot` | Resolves the binding from the canonical identity; fail-closed. |
| Application query | `ResolveOneLevelShippingMethodBindingQuery` + handler + `OneLevelShippingMethodBindingDto` | One binding on the wire — no option list, no provider nesting. |
| Application validator | `ResolveOneLevelShippingMethodBindingQueryValidator` | Primitive-shape rule for the supplied method code. |
| Infrastructure | `ShippingOneLevelCatalogAdminRead.ResolveBindingAsync` | Gate OFF ⇒ zero DB reads; malformed code ⇒ no lookup; `42P01` ⇒ not-ready; unknown code ⇒ unresolved. |
| Endpoints | `GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding` | Additive, admin-authorized read only. |

Guarantees enforced by the resolver:

* the request carries **only** `methodCode`; raw client `serviceCode`/`optionCode` are never accepted;
* the returned pair comes exclusively from the catalog row's immutable legacy binding;
* gate OFF ⇒ `resolved=false` and **zero** database queries;
* unmigrated catalog (PostgreSQL `42P01`) ⇒ `ready=false`, `resolved=false`, never a 500 and never a
  fake success;
* malformed/non-canonical code ⇒ refused **before** any lookup, so `post:ex:press` or
  `post standard;drop` can never be normalized into a new identity;
* canonical but absent code ⇒ `resolved=false` (for example `tipax:express` is never split into
  `(tipax, express)`).

## 3. Tab 2 behavior (Mission B)

`shipping-one-level-rates-tab.tsx` is scoped to the selected method from tab 1:

* **Context banner** names the selected method and its resolved legacy pair, so the operator always
  knows which one-level method is being edited.
* **Read**: `GET /v1/admin/shipping-services/{serviceCode}/rates` for the resolved service.
* **Create / edit / toggle**: existing `createAdminShippingRate`, `updateAdminShippingRate`,
  `setAdminShippingRateActive`, `createAdminShippingCoverageGrant`,
  `setAdminShippingCoverageGrantActive`.
* **Option identity** is always taken from the server binding (`optionCode: resolvedBinding.optionCode`
  on create, `optionCode: policy.optionCode` on update). There is no option `<select>`, no option
  column, and no `groupRatesByOption` grouping — a service-level policy is not rendered under an
  option-bearing method.
* **Tier conditions** reuse `shipping-rate-tier-model.ts` unchanged: grams on the wire, kilograms for
  humans, half-open `[min, max)` boundaries, backend ceilings, exact `1.999 kg → 1999 g`.
* **Destination scope** reuses `ShippingDestinationPicker` and `buildScopeWrite`, so only real
  Geography ids can be submitted.
* **Selection immutability**: a generation guard (`createBindingGenerationGuard`) bumps on every
  method change; in-flight reads *and* writes of the previous method are discarded, so a stale result
  can never be shown for, or saved onto, the new method.
* **Honest states**: no-method, binding-loading, binding-unavailable (gate OFF / not migrated /
  unresolved / invalid response), loading, denied (403), error, ready-with-empty, and a
  service-mismatch guard that refuses a response belonging to another service.
* **Feedback**: in-tab success/error notice plus optional page-level `onNotice`; save/cancel; the
  edit form is reset when the method changes (conservative unsaved-change handling).
* **RTL / keyboard**: the five-tab URL model, `role="tablist"/"tab"/"tabpanel"`, roving `tabIndex`
  and arrow-key handling from 084 are untouched.

Legacy dialogs (`shipping-rates-coverage-dialog.tsx` etc.) and their row actions are **not** deleted;
they remain the gate-OFF route.

## 4. Changed paths

Frontend:
* `src/frontend/app/admin/shipping-services/shipping-one-level-rates-model.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-rates-tab.tsx` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-rates-tab.test.ts` (new)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx` (tab-2 wiring, `onNotice`)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts` (tab-2 wiring task → null)
* `src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts` (wiring assertion)

Backend (Fulfillment only, additive):
* `Tooba.Fulfillment.Application/Shipping/Ports/IShippingOneLevelCatalogAdminRead.cs`
* `Tooba.Fulfillment.Application/Shipping/Queries/ResolveOneLevelShippingMethodBindingQuery.cs` (new)
* `Tooba.Fulfillment.Application/Shipping/Validators/ResolveOneLevelShippingMethodBindingQueryValidator.cs` (new)
* `Tooba.Fulfillment.Application/Validators/FulfillmentValidationCodes.cs`
* `Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogAdminRead.cs`
* `Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs`
* `Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelAdminReadBridgeTests.cs`
* `Tooba.Fulfillment.Tests/Architecture/FulfillmentValidatorCoverageGuardTests.cs`
* `Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs`

No DI change was needed: the new method lives on the already-registered
`IShippingOneLevelCatalogAdminRead`. No schema change, no migration, no seed, no gate switch.

## 5. Tests and results

| Command | Result |
| --- | --- |
| `node --test --experimental-strip-types app/admin/shipping-services/*.test.ts` | 145 passed / 0 failed |
| `npm run typecheck` (frontend) | clean |
| `dotnet test` on `Tooba.Fulfillment.Tests` | 116 passed / 0 failed |
| `dotnet test --filter FulfillmentValidatorCoverageGuardTests` | 8 passed / 0 failed |
| `dotnet test --filter ShippingOneLevelAdminReadBridgeTests` | 21 passed / 0 failed |
| `dotnet test --filter FulfillmentModuleAmsc001W3CertGuardTests` (Host) | **BLOCKED** — see §7 |

Focused coverage added:

* mapping for `post:standard`, inactive `post:express`, `tipax:standard/express`, optionless
  `store_courier`/`in_person`, unmapped and malformed codes (all fail-closed);
* stale method selection (generation guard discards the previous method's in-flight answer);
* tier boundary semantics `1999`/`2000` grams and `[min, max)` (existing tier tests re-run green);
* service-level vs option-specific precedence (a `null`-option policy never matches an
  option-bearing method; grants match on service only);
* gate-OFF parity (zero DB reads, legacy screen still the rendered surface);
* source-level guards: tab 2 has no `<select>`, no option grouping, no `methodCode.split`.

## 6. Gate state, data authority, proof type

* Gate: **OFF** (`Tooba:ShippingOneLevelCatalog` absent from configuration). Unchanged by this task.
* Data authority: Fulfillment owns `method_code` and its legacy binding; Shipping owns rate policies
  and coverage grants; Geography owns destination ids. No cross-module join was introduced.
* Proof type: **source-only**. Tab 2 cannot be exercised live while the gate is OFF and the
  Development one-level tables are unmigrated. Every claimed behavior is proven by focused unit tests
  over the pure decision functions plus the resolver's gate/not-ready/unresolved behavior. No live UI
  or database success is claimed.
* `ShippingRateAuthority`, `ShippingOperationalShadow`, Order, Checkout, Payment, Settlement and
  Geography were not touched.

## 7. Open prerequisites and blockers

1. **Host cert-guard run blocked by a file lock.** `Tooba.Host.Tests` cannot build while the running
   `Tooba.Host` dev process (pid 33548) holds its output DLLs (`MSB3027`/`MSB3021`). Per task rules the
   Host was **not** restarted or killed and the command was not retried repeatedly. The route-count
   expectation was instead verified directly against source: 23 `.Map*` calls across the Fulfillment
   endpoint files (5 + 1 + 8 + 1 + 8), matching the updated guard constant. The guard test itself
   still needs a clean run once the Host is not holding the outputs.
2. **Tab 2 has no live path until the gate is enabled and the one-level tables exist.** The resolver
   correctly reports `ready=false`/`resolved=false` in that state; enabling the gate and running the
   authorized migration is a separate, unauthorized task.
3. Legacy two-level screen remains the operational surface for merchant rate/coverage edits while the
   gate is OFF.

## 8. Rollback

Revert the two commits for this task. The new endpoint and query are additive and unreferenced while
the gate is OFF; reverting the frontend files restores tab 2 to the wiring placeholder. No data,
schema or configuration change needs undoing.

## 9. Actual elapsed

Claim `13:31:45Z` → result `~13:56Z` ⇒ **≈25 minutes**, inside the 30-minute hard timebox.
