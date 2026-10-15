# SHIPPING-MVP-OPTION-PARITY-069 — Option-level rate identity

Task: `TB-SHIPPING-MVP-OPTION-PARITY-069` (program `TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE`, task 7/8).
Parent: `TB-SHIPPING-MVP-SHADOW-PARITY-068`. Base SHA: `59c12a70f66393305f551e6bdaae358430e5713c`.

## Root cause fixed here

068 proved a **structural** parity blocker, not a data gap: a Shipping rate policy was keyed only by
`serviceCode`, while the storefront exposes methods as `{serviceCode}:{optionCode}`. A service with two
options at two different legacy prices (`tipax:standard` 180,000 vs `tipax:express` 260,000) therefore
had no way to reach amount parity under a single `serviceCode` policy — the second option was always an
`AmountMismatch`.

## Canonical identity model

```
policy identity  = (serviceCode, optionCode?, destination scope)
request identity = (serviceCode, optionCode?, destination scope, currency)
```

* `optionCode == null` is the **service-level** policy (unchanged meaning for every pre-existing row).
* `optionCode != null` is an **option-specific** policy.
* `methodCode` is never treated as a `serviceCode`: `tipax:express` is service `tipax` + option `express`.
  The projection now carries `optionCode` as its own field and the shadow candidate carries it separately.

### Deterministic resolution (in this order)

1. **Option tier first.** If any active, covering, option-matching policy is option-specific, the request
   is resolved inside that tier only. Otherwise it falls back to the service-level tier.
2. **Deepest destination scope** inside the winning tier.
3. **Two equally specific rows in the winning tier = `ScopeConflict`** — never "the first row wins".

A policy for option *A* can never price option *B*. A request with no option never matches an
option-specific policy, so every pre-069 caller keeps byte-for-byte behaviour.

### Coverage semantics (explicitly preserved)

Coverage stays a **service + destination** decision: one grant authorizes the whole service at that
destination, including all of its options. Options are a *price* dimension, not an *authorization*
dimension; granting per option would silently shrink already-authorized coverage. Coverage does **not**
widen with option identity, and the engine stays fail-closed (`NotCovered` when no grant exists).

## Schema / migration

`20261015030159_OptionLevelRatePolicy` — additive only:

| change | detail |
| --- | --- |
| new column | `shipping.rate_policies.option_code varchar(64) NULL` |
| dropped index | `ux_rate_policies_service_scope` |
| new index | `ux_rate_policies_service_option_scope (service_code, option_code, country_id, administrative_area_id, city_id, service_area_id) UNIQUE NULLS NOT DISTINCT` |
| new check | `ck_rate_policies_option_code_canonical` (`NULL` or trimmed non-empty) |

`NULLS NOT DISTINCT` is what allows **two tipax options at the same scope** while still refusing two
option-less rows at the same scope. `Down()` drops the column, the constraint and the index and restores
the original index, so the rollback is a single migration revert. No row was rewritten: all seven
pre-existing rows kept `option_code = NULL` and identical behaviour.

## Changed paths (code)

* Shipping Contracts: `Rates/ShippingRatePolicyContracts.cs` (option on request/quote),
  `Admin/ShippingAdminContracts.cs` (option on write request/item), `Errors/ShippingErrorCodes.cs`.
* Shipping Domain: `Aggregates/ShippingRatePolicy.cs` (option identity + `MatchesOption`),
  `Rates/ShippingRatePolicyEngine.cs` (option-first deterministic resolution).
* Shipping Application: `Rates/Ports/IShippingMethodOptionValidator.cs` (new, catalog validation seam),
  `Rates/Commands/ShippingRateCoverageCommands.cs` (create/update validate the method identity),
  `Validation/ShippingAdminValidators.cs` (option shape).
* Shipping Infrastructure: `Rates/FulfillmentShippingMethodOptionValidator.cs` (new adapter over
  `Tooba.Fulfillment.Contracts.IShippingCatalogReader`), `Rates/ShippingRateCoverageDirectory.cs`
  (option-aware duplicate guard + option-immutable update), `Persistence/ShippingDbContext.cs`,
  the new migration, `DependencyInjection/ShippingModule.cs`, and one new Contracts-only project
  reference (Fulfillment.Contracts; no Fulfillment project references Shipping, so it stays acyclic).
* Order: `Storefront/Models/StorefrontOrderModels.cs` (+`OptionCode`),
  `Storefront/Services/StorefrontShippingService.cs` (carry the option code),
  `Storefront/Shipping/RateAuthority/*` (candidate/input/report option identity, explicit per-method
  observer logging), `Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs`.

## Proof

* `log-option-fixtures.txt` — invalid option -> `400 shipping.service_option.unknown`; unknown service ->
  `400 shipping.service_code.unknown`; two option-level tipax rows created at the same scope; the generic
  068 tipax row deactivated.
* `log-observer-069.txt` — one explicit line per method with real numbers:
  `post/standard 120000=120000 Match`, `tipax/express 260000=260000 Match`,
  `tipax/standard 180000=180000 Match`, `store_courier 75000=75000 Match`, `in_person 0=0 Match`.
* `payloads-OFF.json` vs `payloads-ON.json` — identical; legacy projection byte-invariance holds.
* `log-target-proof.txt` — Development target and applied-migration proof.
* `log-test-shipping.txt` (109/109) and `log-test-order-shadow.txt` (31/31).

## Fixtures and rollback

Fixtures (Development-only, `tooba_alpha`, alborz area scope) are **parity fixtures**, not approved
commercial tariffs: `tipax/standard 180000 lead3`, `tipax/express 260000 lead1`; the pre-existing generic
`tipax` area row is deactivated (id kept in `RESULT.bridge.txt`). Rollback = deactivate the two option rows
and reactivate the generic row, plus `dotnet ef database update InitialShipping` for the schema.

## Remaining blocker

`Geography.ResolveLegacyAsync('IR','البرز',null,null)` still returns unresolved from the observer path, so
four of the five projection shapes (everything except the validated-ids shape) stay
`DestinationNotComparable`. This was **not** diagnosed within the timebox and is handed to 070 as a named
blocker. Also not done here: exposing the option field in the admin rates dialog (the source-owned API is
already sufficient to author and edit both options).
