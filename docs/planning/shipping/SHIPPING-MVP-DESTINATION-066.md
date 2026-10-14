# TB-SHIPPING-MVP-DESTINATION-066 — a real customer destination identity path (no cutover)

**Program:** TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
**Mode:** BOUNDED_REAL_CHECKOUT_DESTINATION_IDENTITY_PROPAGATION_NO_CUTOVER
**Parent:** TB-SHIPPING-MVP-DEV-ADMIN-UI-065
**Expected-Base-SHA:** `9f297f43fb8b7faa9290bfb39dd85aef71095a99`
**Verdict:** PASS (scoped)

---

## 1. The real customer destination flow before and after

### Before

The storefront shipping step let a customer pick **a province** (and a city, from a
static list), but only the province ever left the browser:

| Step | Before |
| --- | --- |
| `storefront-shipping.tsx` city `<select>` | updated local form state only; never sent anywhere |
| `loadShippingProjection` | body = `{ cartId, provinceName, methodCode, language }` |
| `StorefrontShippingProjectionRequest` | `CartId, ProvinceName, MethodCode, Language` |
| `ProjectStorefrontShippingQuery` | same five fields |
| `StorefrontShippingShadowObservationInput` | `ProvinceName`, … |
| `StorefrontShippingShadowObserver` | `ResolveLegacyAsync(LegacyCountryCode, ProvinceName, cityName: null, serviceAreaName: null)` |

So the observation identity could never be finer than the **administrative area**, and the
selected city was thrown away at the browser boundary.

### After

| Step | After |
| --- | --- |
| `storefront-shipping.tsx` | the existing projection calls (method select, saved-address select, new-address save) now also pass the selected city: `refreshProjection(province, method, city)` |
| `loadShippingProjection` | body adds `cityName` (nullable) — no new HTTP call was introduced |
| `StorefrontShippingProjectionRequest` | adds optional `CityName`, optional `Destination` (`DestinationIdentifiers`) |
| `ProjectStorefrontShippingQuery` | adds optional `CityName`, optional `Destination` |
| `StorefrontShippingShadowObservationInput` | adds optional `CityName` + optional `Destination` (init-only, so existing positional construction is untouched) |
| `StorefrontShippingShadowObserver` | new `ResolveDestinationAsync`: explicit validated ids win; otherwise the **city** is now forwarded to the canonical resolver |

**Precision rule (no guessing):**

1. **Explicit Geography ids**, when a caller actually has them, are validated with
   `IGeographyLookup.GetByIdsAsync` before use. An empty country or an invalid hierarchy
   returns `null` → `DestinationNotComparable`, and the code deliberately does **not**
   fall back to text resolution (proved by test).
2. **Otherwise** the legacy text (province **and now city**) is handed to
   `ResolveLegacyAsync`, which matches the stored `Code` *or* any localised label exactly,
   scopes the city by the resolved area, derives the city's real parent area, and reports
   `Ambiguous`/`Unresolved` instead of guessing. The observer turns anything other than
   `Resolved` into `DestinationNotComparable`.

The resolver already existed and already accepted `cityName`; the gap was that nothing
ever passed it. This task closes that gap — it does **not** invent city identity from raw
text, and it does **not** add a Geography lookup to the legacy path while the shadow gate
is off.

## 2. Legacy simulation invariance — what was deliberately not touched

* `StorefrontShippingService.ProjectAsync` is untouched and still receives exactly the same
  five arguments. The handler does not forward `CityName` or `Destination` to it, so the
  legacy `ShippingMethodsOptions` quote, method list, selected amount, minimum date,
  delivery windows, draft and revalidation are byte-for-byte the same computation.
* The shadow gate defaults stay `ShadowEnabled = false` / `PrerequisitesConfirmed = false`.
  The observer returns `Disabled` before it looks at any destination, so **zero** extra
  Geography and **zero** Shipping queries occur on the live checkout path (existing test
  `Shadow_off_by_default_performs_no_geography_and_no_shipping_lookup`, still green).
* No Checkout/payment/order-snapshot/pricing/eligibility code was touched. No cutover, no
  shadow parity claim.
* Destination identity remains **observational metadata** only; it flows into
  `StorefrontShippingRateShadowInput.Destination` and stops there.

## 3. Admin cascading picker race (065 residual) — closed

`shipping-destination-picker.tsx` previously fired `loadCities(country, area)` from both
the effect and the country `onChange`, with no ordering guard: a slow country-wide response
could land after a newer area-scoped one and repopulate the city list with every city of the
country (the transient 48-option list seen in 065).

Repair:

* new pure helper `createLatestRequestGuard()` in `shipping-destination-picker-model.ts` —
  each `begin()` opens a generation and returns an `isCurrent()` predicate;
* two independent guards (cascade: country/area → areas + cities; service-area: → service
  areas) so one level can never invalidate the other;
* every loader checks `isCurrent()` **before** writing state, so a stale response is silently
  dropped; clear-stale-descendants behaviour (`clearDependents`) is unchanged;
* the country `onChange` no longer starts its own duplicate fetch — the effect owns loading,
  which also removes one redundant request per country change.

Only the component and its new focused test were touched; no new screen, no broad UI rewrite.

## 4. UI vs HTTP / proof matrix

| Capability | How it was proven |
| --- | --- |
| City name reaches the projection request from a real customer selection | source wiring in `storefront-shipping.tsx` → `loadShippingProjection`; typecheck clean |
| City reaches observation at city precision | `Selected_city_is_forwarded_to_geography_so_identity_reaches_city_precision` (asserts the resolver received `کرج` and the quote request carried `CityId`) |
| Province-only clients unchanged | `Province_only_client_still_resolves_at_area_precision_without_inventing_a_city` (resolver got `cityName = null`, no id validation call, quote destination has no `CityId`) |
| Validated explicit ids win and skip text resolution | `Validated_explicit_destination_ids_win_and_skip_legacy_text_resolution` |
| Invalid ids cannot silently become trusted | `Invalid_explicit_destination_ids_are_not_comparable_and_never_fall_back_to_text_guessing` |
| Ambiguous city is never guessed | `Ambiguous_city_inside_a_resolved_area_is_not_comparable` |
| Shadow off ⇒ zero extra queries | existing `Shadow_off_by_default_performs_no_geography_and_no_shipping_lookup` |
| Picker stale response cannot be displayed | `shipping-destination-picker.test.ts` (3 tests: newest generation wins; stale area response dropped; independent guards) |
| Route alive on the Development host, legacy contract canonical | `log-live-check.txt` — `health 200`, projection returns `401 checkout.authentication_required` (fails closed before any write) |

## 5. Process and data safety

* **No service was restarted.** The Development host identified in 065 (`Tooba.Host.exe`,
  `:5088`, `tooba_alpha`) was left running. It therefore still hosts the pre-066 binary; the
  live check above is a liveness/canonical-error check only, and the new-field behaviour is
  proven by focused tests. This is stated explicitly rather than presented as a live pass.
* **No database writes.** No cart, order, shipping rate or grant row was created or modified;
  no migration was run; no schema change. The live check used an invalid cart id and was
  rejected at the identity gate.
* **No frontend/backend process was killed**, no scratch database or synthetic server created.
* Unrelated pre-existing dirty files were preserved untouched.

## 6. Focused validation

| Suite | Result |
| --- | --- |
| `Tooba.Order.Tests --filter FullyQualifiedName~Storefront` | **121 passed, 0 failed** |
| observer + rate-shadow focused subset | 27 passed |
| `shipping-destination-picker.test.ts`, `shipping-rates-coverage-model.test.ts`, `storefront-shipping-api.test.ts` | 23 passed |
| `npx tsc --noEmit` (src/frontend) | clean |
| architecture guards (import-boundary, flat-folder, feature-boundary, test-discovery) | pass |
| `frontend-source-size.guard.test.ts` | **pre-existing failure**, unrelated: `category-admin-screen.tsx`, `landing-page-composer.tsx`, `storefront-landing-blocks.tsx`, `shared-composition-renderer.tsx` all exceed their baselines, and none of them is a file this task touched. Not "fixed" and the baseline was not widened. |

## 7. Changed paths

| Path | Change |
| --- | --- |
| `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingShadowObservation.cs` | optional `CityName` + `Destination` on the observation input |
| `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingShadowObserver.cs` | `ResolveDestinationAsync` — validated ids first, city-capable legacy resolution second |
| `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs` | query carries `CityName` + `Destination`; legacy call unchanged |
| `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs` | projection request accepts optional `CityName` + `Destination` |
| `src/backend/Modules/Order/Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs` | passes the two optional values through |
| `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingShadowObserverTests.cs` | 5 new focused tests + fake records city/validation calls |
| `src/frontend/app/storefront/storefront-shipping-api.ts` | projection body carries `cityName` |
| `src/frontend/app/storefront/storefront-shipping.tsx` | existing projection calls pass the selected city |
| `src/frontend/app/admin/shipping-services/shipping-destination-picker.tsx` | stale-response guard + single owner for cascade fetch |
| `src/frontend/app/admin/shipping-services/shipping-destination-picker-model.ts` | new pure guard helper |
| `src/frontend/app/admin/shipping-services/shipping-destination-picker.test.ts` | race proof |
| `docs/planning/shipping/SHIPPING-MVP-DESTINATION-066.md`, `docs/architecture/evidence/TB-SHIPPING-MVP-DESTINATION-066/**` | this document + evidence |

## 8. Remaining gaps / honest limitations

* The legacy storefront destination surface still has **no stable city id**: `StorefrontIranGeography`
  is a static catalogue of province codes + city *names*. The `Destination` id seam added here is
  therefore today exercised only by tests; it exists so a caller that does hold validated ids can
  pass them without any further boundary change. Replacing the static catalogue with a
  Geography-backed picker is a separate, larger task and was not attempted.
* `ResolveLegacyAsync` resolves a city only with its area as scope. The storefront UI now sends the
  province, so the scope is real; a client that sent a city without a province would resolve the
  area from the city's own parent (`ResolveCityAsync` with `areaId = null`) and remain unambiguous
  only when the city name is unique in the country — otherwise `Ambiguous` → `NotComparable`.
* The live Development host was not reloaded with the new binary, so no live end-to-end checkout
  projection with a real cart was performed in this task. The projection route also requires
  checkout authentication, so a real customer run needs a genuine cart session.
