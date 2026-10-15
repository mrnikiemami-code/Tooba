# TB-SHIPPING-ONE-LEVEL-TABS-096 — Bounded legacy activation-authority repair

Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_LEGACY_ACTIVATION_AUTHORITY_REPAIR
Base SHA: `c5a72f59b05b5a7c985c3737b5c5f890f9b43496`
Parent task: TB-SHIPPING-ONE-LEVEL-TABS-095 (`BLOCKED_ACTIVATION_AUTHORITY`, accepted)
Status: PASS (dual-writer contained + narrow transactionally consistent option-backed activation proven)
Stop state: STOP_FOR_ARCHITECT
Automatic next task: NONE

## What the Architect ruled

* The operative activation authority stays the legacy Fulfillment columns
  `fulfillment.shipping_services.is_active` and `fulfillment.shipping_service_options.is_active`, because the
  Order storefront reads them.
* `fulfillment.shipping_methods.is_active` is an admin/catalog **projection** and must never become an
  independently competing activation authority.
* For option-backed methods, a narrow identity-preserving update of an existing legacy
  `ShippingServiceOption` is acceptable **only** through one Fulfillment-owned transactional authority that
  also aligns the mapped one-level method row.
* Optionless services are out of scope: activation there means service-level state and must not be guessed.
* No Checkout price cutover, no real Development activation of `post:express` / `snapp_courier`.

## Mission A — dual-writer hazard closed

See `hazard-dual-writer-map.txt` for the full map. Summary:

* Old `UpdateAsync` deleted **all** option rows + their translations and re-inserted them from the request
  body. Three reachable hazards: UUID rewrite (H1), silent activation reset (H2 — the 095 conflict), and
  total option wipe on an option-free body (H3).
* Repaired with `ReconcileOptions`: empty body ⇒ no option management; match by echoed id else by stable
  `code`; update the same row in place; activation applied only when the client asserted that option's
  identity; per-option translation replacement; genuine removals (non-empty body, option absent) preserved.
* Only one production caller of `UpdateAsync` exists (`UpdateShippingServiceCommand` ←
  `PUT /v1/admin/shipping-services/{serviceId}`), so the repair is contained.

## Mission A — narrow activation operation (option-backed only)

New single authority, no second writer:

| Layer | Artifact |
| --- | --- |
| Port | `Application/Shipping/Ports/IShippingOneLevelCatalogAdminActivation.cs` |
| Command + handler | `Application/Shipping/Commands/SetOneLevelShippingMethodActivationCommand.cs` |
| Validator | `Application/Shipping/Validators/SetOneLevelShippingMethodActivationCommandValidator.cs` |
| Infrastructure | `Infrastructure/Shipping/ShippingOneLevelCatalogActivation.cs` |
| DI | `Infrastructure/DependencyInjection/FulfillmentModule.cs` |
| Endpoint | `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/activation` in `Endpoints/Shipping/ShippingServiceEndpoints.cs` |

Contract properties, all enforced in the new operation:

* Input is **only** the immutable one-level `methodCode` plus the requested `IsActive`. No `serviceCode`,
  no `optionCode`, no option UUID is accepted.
* The legacy binding is resolved server-side from the method row (`LegacyServiceCode`, `LegacyOptionCode`),
  which the operation never rewrites.
* Refusals before any write: gate OFF, blank/raw/invalid code, unknown method, missing legacy parent,
  inactive legacy parent (never auto-activated), optionless method (never guessed), missing/stale option row.
* On success, exactly two rows change in **one** `SaveChangesAsync`: the existing legacy
  `ShippingServiceOption` (via the existing domain setter `SetActive`) and the one-level
  `ShippingMethod` projection. Codes, sort orders, translations and sibling options are untouched.
* Idempotent: requesting the already-effective state writes nothing (`NoChange`).
* Gate OFF ⇒ zero DB calls. Missing table ⇒ controlled `CatalogNotReady`, not a 500.
* `Contracts` error codes could not be added (path not authorized in 096); only pre-existing stable codes
  are reused.

## Mission B — proof actually run

* `dotnet test …/Tooba.Fulfillment.Tests --filter ShippingOneLevelActivationTests|ShippingServiceDirectoryDualWriterTests`
  → 14/14 passed (10 activation + 4 dual-writer).
* Guard updates kept narrow and executed: `FulfillmentValidatorCoverageGuardTests` (18→19 endpoint-reachable
  requests, 12→13 required validators, Shipping/Validators 6→7 files) and
  `FulfillmentModuleAmsc001W3CertGuardTests` (module-owned routes 24→25).
* Full Fulfillment module suite → 154/154 passed.
* Host cert guard + legacy `ShippingServiceAdminTests` → 23/23 passed.
* Logs: `tests-fulfillment-focused.txt` (focused), reproduced summary above. No live DB DML was executed; all
  behaviour was proven on the in-memory Fulfillment context.

## Safety / gate state

* `Tooba:ShippingOneLevelCatalog` gate unchanged. `Tooba:ShippingRateAuthority` and
  `Tooba:ShippingOperationalShadow` neither read nor changed.
* No live Development activation DML. `post:express` and `snapp_courier` were NOT activated anywhere.
* No migrations, no schema change, no seed, no permissions bypass, no Host/Next restart, no production or
  staging contact.
* Checkout price authority untouched (no cutover).

## Remaining issues (for the Architect)

1. **Frontend toggle not built** (out of scope for 096 by instruction). The new endpoint has no UI yet.
2. **Optionless activation still undecided.** `store_courier`, `in_person`, `snapp_courier` map to a service
   with no option; the operation refuses them. A separate ruling is needed (service-level activation is a
   broader storefront authority question).
3. **Contracts error codes** for `inactive parent` / `optionless` / `stale option` should be distinct; that
   requires authorizing `Tooba.Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs`.
4. **Concurrent lost-update across the two surfaces** is narrowed but not eliminated: an id-echoed legacy
   form save that is stale still applies its carried `isActive`. Eliminating that fully requires an
   optimistic-concurrency token / explicit option-management intent on the legacy write contract (frontend +
   Contracts change) — reported, not implemented.
