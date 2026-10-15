# TB-SHIPPING-ONE-LEVEL-TABS-100 — Evidence index

Task-ID: `TB-SHIPPING-ONE-LEVEL-TABS-100` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-099`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor
Program: `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode: `BOUNDED_STOREFRONT_ONE_LEVEL_LABEL_PARITY`
Base SHA: `ec5e693376b56d94441e017df4783e136c63525a` (== `origin/main` at claim, ancestor verified)
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask = NONE`

## Contents

| File | Proof |
| --- | --- |
| `label-before-after.sql` / `label-before-after.txt` | Read-only SQL + output: real legacy service/option names vs one-level `shipping_method_translations` names in `tooba_alpha` (no DML) |
| `tests-fulfillment.txt` | `Tooba.Fulfillment.Tests` full suite: 158 passed / 0 failed |
| `tests-order.txt` | `Tooba.Order.Tests` full suite: 316 passed / 0 failed |
| `tests-host-focused.txt` | `Tooba.Host.Tests` filter `Architecture\|Storefront\|Shipping`: 1508 passed / 45 failed (pre-existing) |
| `tests-host-focused-failed-names.txt` | The 45 failing names (all documented pre-existing drift, none label/one-level) |
| `changed-files.txt` | Exact task-owned changed paths |
| `shas.txt` | Base / code / evidence SHAs |
| `summary.md` | Result summary, invariants, scope, elapsed, rollback |

## Focused tests added

- `Tooba.Order.Tests/Storefront/StorefrontShippingOneLevelLabelTests.cs` — drives the real
  `StorefrontShippingService.ProjectAsync` with fakes for cart, catalog, languages, options, seed,
  drafts, actor and clock:
  - gate OFF → legacy labels `پست قدیمی — استاندارد قدیمی`, `پست قدیمی — اکسپرس قدیمی`,
    `اسنپ قدیمی`; method codes `post:standard`, `post:express`, `snapp_courier`; prices `120000`,
    `200000`, `90000`; lead days `4`, `2`, `0`; option codes `standard`, `express`, `null`.
  - gate ON (map `post:standard → «پست استاندارد یک‌سطحی ۰۹۴»`) → only that label changes; the other
    two labels, all codes, prices, lead days, service code, option code and `IsFree` are identical to
    the gate-OFF run.
  - requested language `en` → the resolved English `LanguageId` reaches the seam; because the
    seam answers empty for that language the projection keeps the exact legacy label.
  - unknown key (`post:economic`) and optionless-service key (`post`) in the map → no label is
    rebuilt or substituted; legacy labels are kept verbatim.
- `Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelStorefrontLabelReadTests.cs` — seam semantics:
  gate OFF ⇒ empty map **and zero catalog reads**; gate ON ⇒ exact-language translation only
  (fa name for fa, en name for en, never cross-language); unknown code ⇒ one catalog read and no
  label; whitespace code and `Guid.Empty` language ⇒ empty **before any DB read**;
  `PostgresErrorCodes.UndefinedTable` ⇒ empty map (fail-closed), no exception.

## Method/price invariant evidence

The substitution runs strictly after `LoadEligibleMethodsAsync` has built the legacy-eligible list and
only rewrites `Label` via `with { Label = … }`. The Order tests assert equality of
`MethodCode`/`ServiceCode`/`OptionCode`/`PriceAmount`/`LeadDays`/`IsFree` between gate OFF and gate ON
and against the hardcoded legacy rate configuration, so no eligibility, amount or identity change is
possible through this path.

## Pre-existing unrelated failures (not caused by this task)

`Tooba.Host.Tests` focused run: 45 failures, all in the documented pre-existing drift set —
`Host/Admin` file count `17` vs legacy assertions `15`/`52`, legacy guards reading deleted
`Tooba.Catalog.Domain/CatalogDomain.cs` and `CatalogCategoryTreeRules.cs`, and
`PaidProjectionFinancialTests.Settlement_and_initiate_exclude_store_shipping_from_seller_ids`
(recorded as pre-existing in `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` and in the
TB-TMAR-TAX/SETTLEMENT/PROMOTION W2–W3 baseline failing-name files). No failing test name mentions
one-level, label projection or the files changed here; zero new failures, zero silently fixed.

## Unproven / not performed (honest scope)

- No frontend, Host, migration, schema, configuration or financial-module edit.
- No shared Development DML, no real activation, no merchant label edit on `tooba_alpha`, no
  `:5088`/`:3000` Host restart, no live order/checkout write.
- Real `en` label on shared data was not exercised end-to-end (only in unit tests); the storefront
  page/Checkout were not re-rendered in this task by explicit prohibition.
