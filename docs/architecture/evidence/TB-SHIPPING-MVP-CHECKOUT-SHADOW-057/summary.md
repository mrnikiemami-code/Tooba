# TB-SHIPPING-MVP-CHECKOUT-SHADOW-057 — evidence summary

- Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor · Program: `TOOBA-SHIPPING-MVP-FINAL-THREE`
- Bridge-Task-Row: `9a5f5a6c-cc8c-49a8-bb06-cd9424af05bb`
- Expected-Base-SHA: `d734802ed6a7c823c778714cabd6376762d3fa6f`
- Base-SHA: `ba13a97a5678fb86953882d4913ef46a9f4d1878` (only 056 evidence/bookkeeping drift over expected base)
- Mode: `BOUNDED_CHECKOUT_SHADOW_INTEGRATION_NO_UNPROVEN_CUTOVER`
- Status: **PASS** (worker). No live/runtime evidence; no cutover; Host was neither restarted nor deployed.

## Changed paths

```text
src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingShadowObservation.cs   (added)
src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingShadowObserver.cs        (added)
src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs (edited)
src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs                                                          (edited)
src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingShadowObserverTests.cs                              (added)
docs/planning/shipping/SHIPPING-MVP-CHECKOUT-SHADOW-057.md                                                                    (added)
docs/architecture/evidence/TB-SHIPPING-MVP-CHECKOUT-SHADOW-057/**                                                             (added)
```

No other file was touched. `Expected-Base-SHA` is an ancestor of `Base-SHA`; the only intervening commits are
056 evidence/bookkeeping (`docs/architecture/evidence/TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056/**`), so no
overlapping source changed. Pre-existing dirty/untracked files were preserved (no reset/stash/rebase/force).

## Gate / DI proof

* `OrderModule.AddServices` binds `Tooba:ShippingRateAuthority` into
  `StorefrontShippingRateAuthorityOptions` and registers `StorefrontShippingRateShadowEvaluator` (with
  `IShippingRateQuoteLookup` via `GetService`, i.e. Contracts-only and optional) plus
  `StorefrontShippingShadowObserver` exactly once (`log-*`, guard test
  `Order_module_registers_the_shadow_seams_once_from_existing_composition`).
* `appsettings*.json` is untouched and has no `Tooba:ShippingRateAuthority` section, so options are the safe
  defaults (`ShadowEnabled = false`, `PrerequisitesConfirmed = false`) → gate `Disabled`.
* Guard test `Gate_option_defaults_stay_off_and_the_authority_is_still_disabled`.
* Guard test `Real_projection_query_observes_the_shadow_and_returns_the_untouched_result`.
* Guard test `Shadow_report_has_no_path_to_price_draft_or_snapshot_authority`.

## Test outcomes

| Log | Command | Result |
| --- | --- | --- |
| `log-build-order-infrastructure.txt` | `dotnet build Tooba.Order.Infrastructure -c Debug` | Build succeeded, 0 errors |
| `log-test-order-module.txt` | `dotnet test Tooba.Order.Tests -c Debug` | **281 passed / 0 failed** |
| `log-test-shadow-narrow.txt` | `dotnet test Tooba.Order.Tests --filter FullyQualifiedName~StorefrontShippingShadow` | **18 passed / 0 failed** |
| `log-test-shipping.txt` | `dotnet test Tooba.Shipping.Tests -c Debug` | **82 passed / 0 failed** |
| `log-build-host-tests-redirected.txt` | `dotnet build Tooba.Host.Tests -c Debug -o %TEMP%` (Host bin locked by running Host) | Build succeeded, 0 errors |
| `log-test-host-architecture-redirected.txt` | `dotnet vstest %TEMP%/…/Tooba.Host.Tests.dll --TestCaseFilter Architecture` | **Not valid evidence**: 1383 failures are path-relative (guards walk up from `AppContext.BaseDirectory`, which is the redirect dir), not real regressions. Running Host guards from their real output path requires restarting the Host, which this task forbids. |

## Shadow comparisons (per-candidate outcomes available; one stable code recorded)

The seam records exactly one `StorefrontShippingShadowObservationCode` per projection:
`Disabled` | `PrerequisitesUnconfirmed` | `ShadowUnavailable` | `DestinationNotComparable` | `Observed`,
with per-candidate `StorefrontShippingRateShadowOutcome` names appended in a bounded set (≤7 values).
No price, promise, draft or snapshot value is produced, read or written by the seam — proven by unit tests
(`Mismatch_and_currency_mismatch…`, `Missing_coverage_is_reported_and_never_becomes_allowed_or_free`,
`Real_projection_query_observes_the_shadow_and_returns_the_untouched_result`,
`Shadow_report_has_no_path_to_price_draft_or_snapshot_authority`).

## Non-live / runtime status

The database was not queried, no DDL/DML was executed, no rate policy or coverage grant was created, no
migration/registry/seed/appsettings/Host composition file changed, no running process was touched, and the
Host was not restarted or deployed. Therefore no claim is made that checkout traffic has switched to the new
authority — it has not, and this task creates no way to switch it.
