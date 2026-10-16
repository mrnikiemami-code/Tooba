# TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155 — result

Status: **PASS** (implementation + focused proof complete) — STOP_FOR_ARCHITECT
Implementation SHA: `2872dea9590eacd192d09c08873e55ebff0e2c12` (base `3134141cff468b37401a11100ce1bf2606c7f4e6`);
the evidence folder is committed in the follow-up evidence commit (HEAD == origin/main verified before reporting).
Channel: `tooba-main` · Worker: `tooba-worker-01` · Mode: BOUNDED_IMPLEMENT_PER_METHOD_AUTHORITATIVE_MONEY_STATUS

## Single deliverable

One additive, read-only seam owned by the Shipping module exposes a typed financial status of ONE
canonical shipping method, derived from the real persisted rate/coverage rows, and the Admin General
tab renders it. `isActive` is never used to infer financial readiness, empty data is never "free",
and a provider/service-level legacy row is never projected as an option method's tariff.

### New contract (owner: Shipping)

`GET /v1/admin/shipping-services/{serviceCode}/financial-status?optionCode=<option|omitted>`

* Authorization: existing admin permission `fulfillment.view` through `IShippingAdminAuthorizer`
  (401/403 fail closed, no anonymous path, no `Manage` permission used, read-only GET).
* `optionCode` omitted/empty ⇒ service-level (optionless) method identity.
* Response (`ShippingMethodFinancialStatusView`): `state` (`NeedsConfiguration` /
  `FinanciallyConfigured` / `LegacySharedUnresolved` / `Conflicting`), `mode` (`None` /
  `FixedAmount` — the only schema-provable mode), `currencyCode` (only when a single currency is
  provable), `ruleCount`, `conditionedRuleCount`, `hasZeroChargeRule`,
  `activeServiceLevelRuleCount`, `activeSiblingOptionRuleCount`, `activeCoverageGrantCount`, `rules[]`
  (owned rules only), `reasonCode`.

### Evaluation rules (deterministic, no IO, no current time)

`ShippingMethodFinancialStatusEvaluator` (Application read model):

1. Owned = active rows whose `serviceCode` AND `optionCode` equal the authoritative identity.
2. No owned row and the method has an option while service-level rows exist ⇒
   `LegacySharedUnresolved` (`shipping.financial.legacy_service_level_shared`), rules empty, **no
   amount, no currency, no free**.
3. No owned row and no service-level row ⇒ `NeedsConfiguration`
   (`shipping.financial.no_owned_rules`).
4. Owned rows in more than one currency, or two owned rows with the same scope+condition ⇒
   `Conflicting` (`mixed_currency` / `ambiguous_owned_scope`), no single amount asserted.
5. Otherwise `FinanciallyConfigured` (`owned_rules_present`) with `Mode = FixedAmount`. A stored zero
   is reported as `hasZeroChargeRule` only — the schema has no approved free mode.
6. Optionless methods own their service-level rows (never reported as legacy shared).

## Supported monetary modes (evidence)

See `monetary-modes-table-155.md`. Summary: `FIXED_AMOUNT` provable; `RULE_BASED_RATE` partial
(destination/tier-scoped fixed amounts, no expression engine); `FREE_TO_CUSTOMER`,
`COLLECT_ON_DELIVERY`, `PROVIDER_QUOTE` **not** provable from the current schema/owner and therefore
never returned or displayed.

## Admin representation (General tab, unchanged four tabs / RTL / languages)

* Header badge now shows the server-verified financial state (`مالی: تنظیمشده` / `نیازمند تنظیم` /
  `تعرفهٔ مشترک قدیمی` / `متناقض`), separately from the activation badge.
* A "وضعیت مالی مشتری" section shows: the precise reason, the proven money line only when a single
  owned rule in a single currency is authoritative, and explicit caveats for shared legacy rows,
  sibling rows, absent coverage and a stored zero amount.
* A compact "حالتهای مالی اثباتپذیر در اسکیمای فعلی" disclosure states the provable/not-provable
  modes.
* No client-side splitting of a method code: the identity comes from the existing server binding
  resolver. If the read is unavailable/denied/error, the tab claims nothing
  (`FINANCIAL_STATUS_UNAVAILABLE_FA`, `data-testid=admin-shipping-method-financial-unavailable`).

## Changed files

Backend (Shipping module only):
* `Tooba.Shipping.Contracts/Admin/ShippingMethodFinancialStatusContracts.cs` (new)
* `Tooba.Shipping.Application/Rates/ReadModels/ShippingMethodFinancialStatusEvaluator.cs` (new)
* `Tooba.Shipping.Application/Rates/Queries/GetShippingMethodFinancialStatusQuery.cs` (new)
* `Tooba.Shipping.Application/Validation/ShippingAdminValidators.cs` (+ query validator)
* `Tooba.Shipping.Endpoints/Admin/ShippingMethodFinancialStatusEndpoints.cs` (new)
* `Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs` (maps the new read route)
* `Tooba.Shipping.Tests/Admin/ShippingMethodFinancialAuthorityTests.cs` (new, 14 tests)

Frontend (`src/frontend/app/admin/shipping-services/`):
* `shipping-method-financial-model.ts` (new, pure mapping/render model)
* `shipping-method-financial-api.ts` (new, authenticated read client)
* `shipping-method-financial-model.test.ts` (new)
* `flat-general-tab.tsx` (verified status section; still writes only the two locked seams)

Docs: this evidence folder + `docs/ai/tasks/TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155.task.md`.

## Focused verification (real code paths, no fake readiness badges)

| Check | Result |
| --- | --- |
| `ShippingMethodFinancialAuthorityTests` (real `ShippingDbContext` + real `ShippingRateCoverageDirectory`) | 14/14 PASS (`backend-focused-tests-155.txt`) |
| Full Shipping test project | 303 PASS / 1 FAIL — failure is PRE-EXISTING and unrelated (see below) (`backend-shipping-suite-155.txt`) |
| Admin mapping/render + pinned General-tab guards (5 files) | 53/53 PASS (`frontend-focused-tests-155.txt`) |
| `npx tsc --noEmit` | exit 0 (`frontend-typecheck-155.txt`) |
| Shipping endpoints project build | success, 0 warnings / 0 errors |

Isolation and fail-closed coverage proven by tests: `tipax:standard` vs `tipax:express`,
`post:standard` vs `post:express`, missing/ambiguous owned policy, active-but-no-coverage,
optionless ownership, deactivated rule, stored zero ≠ free, invalid binding/authorization
fail-closed (binding unresolved ⇒ no request; 401/403 ⇒ nothing displayed).

## Pre-existing red (NOT caused by this task, not repaired here)

1. `Tooba.Shipping.Tests.Compatibility.OperationalQuoteCompatibilityTests.No_module_outside_shipping_references_the_operational_quote_types`
   — the Order-side shadow observer (`StorefrontShippingOperationalShadowObserver.cs` /
   `OrderModule.cs`) references `IShippingOperationalQuoteLookup`/`ShippingOperationalQuote` since
   commit `91951b8` (TB-SHIPPING-FULL-OPERATIONS-079), which that guard forbids. Provenance captured in
   `preexisting-red-provenance-155.txt`; the offender files are untouched and clean in this tree.
2. `src/frontend/lib/architecture/frontend-source-size.guard.test.ts` — four unrelated oversized files
   are above their baselines (`category-admin-screen.tsx` 2230>2215, `admin-landing-page-composer.tsx`
   1442>1440, `storefront-landing-blocks.tsx` 1033>1026, `shared-composition-renderer.tsx` 918>907).
   All four are clean (unmodified) in this working tree; the new files added here are 65–303 LOC,
   far below the 800-LOC threshold.

## UNVERIFIED in this timebox (explicitly not claimed)

* **No live HTTP/browser round trip** of the new read route was performed. The 45-minute hard timebox
  was consumed by the bounded implementation + focused tests; the shared `:3000`/`:5088` stack and
  `tooba_alpha`/`store-alpha` are read-only and must not be restarted, and the running shared build
  does not contain this new route, so a live sample would require standing up a separate isolated
  Host build. Therefore no real HTTP status/JSON sample is recorded here, and the Admin rendering was
  verified by source-pinned guards + pure mapping tests rather than by screenshot.
  Route mapping/authorization is instead pinned by source guard tests (exactly one `MapGet`, exactly
  one `RequireAuthorizedAsync` with `fulfillment.view`, no write verb, no `AllowAnonymous`).
* Live production customer acceptance, carrier calendar/origin authority and the financial
  policy EDIT/WRITE increment remain out of scope (backlog A–E of the task envelope).

## Boundaries respected

* No production price calculation, customer quote, ledger, carrier payable, allocation, payout,
  checkout tax, order snapshot, schema, migration or storefront behavior changed.
* Contracts-only cross-module access preserved; no Host `DbContext`, no business logic in Host, no
  module-boundary or authorization weakening; no new write seam; no client-side method-code split.
* Validation/backwards compatibility preserved: the two locked General-tab writes are unchanged, the
  existing rate/coverage/packaging/schedule/operational routes keep their exact shapes.
* Unrelated dirty files and stashes preserved; commit fast-forward; `HEAD == origin/main`.

## Realistic next step (Architect decision)

1. Authorize a live isolated Host/browser acceptance of this read route (screenshot + real JSON) so
   the visual/HTTP layer is proven, not only tested.
2. Then proceed with backlog A (method-specific financial policy EDIT/WRITE + opt-in restriction
   defaults), which this read seam now makes safe.
