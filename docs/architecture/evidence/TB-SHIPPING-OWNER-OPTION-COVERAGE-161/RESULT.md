# TB-SHIPPING-OWNER-OPTION-COVERAGE-161 — RESULT

Task: `TB-SHIPPING-OWNER-OPTION-COVERAGE-161`
Parent: `TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: `cursor`
Base SHA (expected): `0ad0296ab0df0c2449417cf6438310b1cf3ca6ff`
Code SHA: `c08f232406dccc63eebdaf148e12458f2eeb73bb`
Verdict: **PASS** (owner-side semantics + persistence model proof); see §7 for the one deliberate, contract-mandated supersession.

---

## 1. What was built (production, not a report)

Owner-side geographic restriction is no longer service-only. Each canonical flat method
(`serviceCode` + optional `optionCode`) now owns its own restriction decision:

| Piece | Change |
|---|---|
| `ShippingCoverageGrant` | additive nullable `OptionCode`; legacy rows keep `NULL` = service-level. `BelongsToMethod` compares exact method identity (no fallback). |
| `ShippingMethodCoverage` (new aggregate, table `shipping.method_coverages`) | explicit `ShippingCoverageMode` per method identity: `Unrestricted` (default meaning) / `Restricted`. Unique over `(service_code, option_code)` with `NULLS NOT DISTINCT`. |
| `ShippingRatePolicyEngine.Quote` | new additive 4-arg overload taking declarations; 3-arg overload delegates with `null` so every existing caller is byte-compatible. |
| `ShippingRateQuoteLookup` | loads the per-service declaration slice and passes it to the engine. |
| `ShippingRateCoverageDirectory` | option-aware grant create/duplicate check/mapping, declaration upsert, declaration read in the Admin view. |
| Contracts/Admin + Application | `ShippingCoverageGrantWriteRequest.OptionCode`, `ShippingCoverageGrantAdminItem.OptionCode`, `ShippingMethodCoverageWriteRequest/AdminItem`, `ShippingRatesAndCoverageView.MethodCoverages`. New `IShippingMethodCoverageDirectory` port (separate from rate/coverage write seam so existing stubs stay valid). |
| Endpoint | `PUT /v1/admin/shipping-services/{serviceCode}/method-coverage` under the existing `fulfillment.manage` gate (no new permission, no anonymous route). |
| Migration | `20261016211322_OptionScopedCoverageRestriction` + designer + model snapshot. No backfill, no migration of legacy rows to options, no universal destination row. |

`ShippingReasonCodes.CoverageMethodRestricted = "shipping.coverage.method_restricted"` distinguishes a
method-restriction denial from the legacy `coverage.missing`.

## 2. Locked semantics as implemented

- **Unrestricted is the absence of a restriction, not the absence of a quote.** No declaration (or an
  explicit `Unrestricted`) never produces a coverage failure; the owned rate and every other
  authoritative constraint still decide the quote.
- **Restricted enforces only that method.** Allowed set = active grants with the *same* service + option.
  A sibling option's grants, and provider/service-level grants, can neither authorize nor narrow it.
- **Empty allowed set = nowhere.** `Restricted` + zero grants denies every destination; it never decays
  to "unrestricted" (no sentinel/universal destination row exists).
- **Legacy is untouched.** Default `LegacyFallback` still requires an active *service-level*
  (`option_code IS NULL`) grant; option-scoped grants are not consulted, so adding the column cannot
  widen or narrow legacy coverage.
- **Pricing authority unchanged.** `StrictOwnedOption` still never inherits a provider-level or sibling
  rate (TB-160); a conditioned rate without a trusted measurement stays `MeasurementRequired`.
- **Coverage/eligibility ≠ pricing readiness.** No customer-facing gate, no Order/storefront change, no
  "enabled = quote-ready" conflation; the Admin write is a coverage decision only.

## 3. Files changed

```
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Rates/ShippingRatePolicyContracts.cs
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Admin/ShippingAdminContracts.cs
src/backend/Modules/Shipping/Tooba.Shipping.Domain/Aggregates/ShippingCoverageGrant.cs
src/backend/Modules/Shipping/Tooba.Shipping.Domain/Aggregates/ShippingMethodCoverage.cs      (new)
src/backend/Modules/Shipping/Tooba.Shipping.Domain/Rates/ShippingRatePolicyEngine.cs
src/backend/Modules/Shipping/Tooba.Shipping.Application/Rates/Commands/ShippingRateCoverageCommands.cs
src/backend/Modules/Shipping/Tooba.Shipping.Application/Rates/Ports/IShippingMethodCoverageDirectory.cs (new)
src/backend/Modules/Shipping/Tooba.Shipping.Application/Validation/ShippingAdminValidators.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/ShippingDbContext.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261016211322_OptionScopedCoverageRestriction.cs (new)
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261016211322_OptionScopedCoverageRestriction.Designer.cs (new)
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/ShippingDbContextModelSnapshot.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Rates/ShippingRateCoverageDirectory.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Rates/ShippingRateQuoteLookup.cs
src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Rates/ShippingMethodCoverageRestrictionTests.cs (new, 11 tests)
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Rates/ShippingStrictOwnedOptionRateTests.cs
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Persistence/ShippingPersistenceModelTests.cs
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingAdminEndpointOwnershipTests.cs
docs/ai/tasks/TB-SHIPPING-OWNER-OPTION-COVERAGE-161.task.md
```

No file outside `src/backend/Modules/Shipping/**` and the task artifact was touched. Pre-existing dirty
files (`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-075/-079/*.txt`,
`src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx`) were left unmodified and uncommitted.

## 4. Focused test evidence (`test-runs-161.txt`)

- `Tooba.Shipping.Tests`: **329 passed, 1 failed, 330 total**, 1 s.
  - Only failure: `OperationalQuoteCompatibilityTests.No_module_outside_shipping_references_the_operational_quote_types`
    — **pre-existing red**, caused by unmodified `Order` files
    (`Tooba.Order.Infrastructure/...`, `Tooba.Order.Application/...`); none of them are in this change set
    and `git status` shows them unmodified. Documented, not introduced here.
- `ShippingMethodCoverageRestrictionTests`: 11/11 pass — restricted allow/deny, sibling isolation,
  provider-grant non-inheritance, empty allowed set = deny all, missing declaration = unrestricted but
  rate-mandatory, `MeasurementRequired` preservation, legacy parity (option-scoped grant neither widens
  nor narrows), service-level declaration scoping, and an InMemory roundtrip that writes the declaration
  + option-scoped grant through the real directory and quotes through the real adapter.
- `ShippingPersistenceModelTests`: model/table/index/constraint/migration-chain pins updated and green
  (6 migrations, `NULLS NOT DISTINCT` count now 8, new `ck_method_coverages_*` + identity index).
- `ShippingStrictOwnedOptionRateTests`: TB-160 rate-authority tests green after the supersession in §7.
- `dotnet build Tooba.Order.Application`: 0 errors — the additive overload kept every legacy caller
  compiling.

## 5. Persistence / migration proof

`20261016211322_OptionScopedCoverageRestriction` was generated from the real model (`dotnet ef
migrations add`), so the designer + snapshot are model-derived rather than hand-written. It:

1. drops only `ux_coverage_grants_service_scope`;
2. adds nullable `coverage_grants.option_code` (+ canonical CHECK) — every existing row keeps `NULL`,
   i.e. remains a legacy service-level grant;
3. re-creates the grant identity index as `(service_code, option_code, country_id, administrative_area_id,
   city_id, service_area_id)` `NULLS NOT DISTINCT`;
4. creates `shipping.method_coverages` with PK, canonical checks, `mode IN (0,1)`,
   `ux_method_coverages_service_option` (`NULLS NOT DISTINCT`) and `ix_method_coverages_service`.

No data migration, no backfill, no legacy-row-to-option assignment, no shared-DB DDL was run.

**Not proven in this task:** execution of the migration against a live PostgreSQL instance. The task
forbids migrations on the shared DB and the 45-minute timebox did not fit an isolated scratch container
with the full Host schema. The proof delivered is the offline one the module already relies on
(model ↔ snapshot in sync via `HasPendingModelChanges()`, full generated DDL script assertions, migration
chain pinning). Wiring a scratch-DB `Up`/`Down` run is the remaining prerequisite below.

## 6. Timings

| Step | Elapsed |
|---|---|
| Inspection + claim | ~6 min |
| Domain/contracts/engine/application/infrastructure edits | ~17 min |
| `dotnet ef migrations add` + model review | ~3 min |
| Focused test authoring + 3 fix/run cycles | ~13 min |
| Evidence + commit + push | ~5 min |
| **Total** | **~44 min of the hard 45-minute box** |

One full Shipping suite + one Order compile were used; no repeated full suites.

## 7. Deliberate supersession (Architect attention)

TB-160 test `Strict_mode_without_coverage_fails_closed` asserted that strict mode with **no grants at
all** is `NotCovered`. TB-161's locked contract states:

> "Missing explicit restriction is NOT a coverage.missing failure for strict mode; owned rate remains
> mandatory."

Those two cannot both hold. The test was therefore rewritten (comment records the supersession) to assert
the new intent: strict mode with an owned rate and no declaration is **Quoted**, and narrowing now comes
only from an explicit `Restricted` declaration (proven by the new tests). All TB-160 *rate*-authority
assertions (no provider/sibling rate inheritance) were left untouched and stay green.

If the Architect intends instead to keep an implicit service-level coverage gate in strict mode, that is a
one-line engine change, but it must supersede the TB-161 contract sentence above.

## 8. Remaining prerequisites for the Order customer quote cutover

1. Isolated scratch PostgreSQL run of `OptionScopedCoverageRestriction` `Up` then `Down` (`NULLS NOT
   DISTINCT` requires PG15+), with row-count parity for pre-existing `coverage_grants`.
2. Decide the read/write Admin surface for declaring `Restricted` allowed destinations per method
   (`coverage-grants` with `OptionCode` is ready; the next task must decide whether the UI shows the
   declaration state as a third state per method tab).
3. Order-side wiring: request `StrictOwnedOption` from the storefront quote path **and** map
   `CoverageMethodRestricted` to a customer-visible "not available at this destination" state (currently
   no Order file was changed, per scope).
4. Extend the `tipax:standard` / `tipax:express` data set with real owned rates so the unrestricted
   default cannot be mistaken for a quote-ready method.
