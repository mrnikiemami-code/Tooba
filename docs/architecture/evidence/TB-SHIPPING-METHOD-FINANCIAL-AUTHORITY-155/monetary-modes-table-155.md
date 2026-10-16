# Supported monetary modes — evidence table (TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155)

The locked vocabulary has five modes. This table states which of them the CURRENT authoritative owner
(Shipping module: `ShippingRatePolicy` / `ShippingCoverageGrant`) can actually PROVE. Nothing outside
this table is claimed anywhere in the new read model or in Admin.

| Locked mode | Provable today? | Evidence in the current schema / owner | How the new read model treats it |
| --- | --- | --- | --- |
| `FREE_TO_CUSTOMER` | **NO** | No mode column, no zero-price rule type. `ShippingRatePolicy.FreeAboveThreshold` is a *conditional* threshold on a paid rule, not an explicit free policy. | Never returned as a mode. A stored `ChargeAmount = 0` is reported only as the data fact `HasZeroChargeRule = true`, with the explicit statement that the schema has no approved free mode. |
| `FIXED_AMOUNT` | **YES** | `ShippingRatePolicy.ChargeAmount` + `CurrencyCode` on an active row owned by the method identity (service + option). | `Mode = FixedAmount`; the single amount is shown only for exactly one owned rule in a single currency. |
| `RULE_BASED_RATE` | **PARTIAL** | Rows can be scoped by destination depth and by weight/item-count tiers, but there is no expression/rule engine: each row is still a declared fixed amount. | Reported as `RuleCount` / `ConditionedRuleCount` over fixed-amount rules. No calculator/expression mode is claimed. |
| `COLLECT_ON_DELIVERY` | **NO** | No payer-responsibility / collect-at-delivery / advance-charge field exists on rate, quote or coverage models. | Never returned. |
| `PROVIDER_QUOTE` | **NO** | No carrier-quote integration entity, validity or failure-rule model exists in Shipping. | Never returned. |

## Identity rule applied by the read model

* Method identity is the authoritative pair `(serviceCode, optionCode)` obtained from the server's
  one-level binding resolver (`GET …/shipping-methods/one-level/{methodCode}/binding`); a method code is
  never split in the browser or in the endpoint.
* An active row is **owned** by the method only when service and option match exactly
  (`optionCode` null with null).
* Active service-level rows (`optionCode = null`) for an option-bearing method are NOT its tariff.
  They surface as `LegacySharedUnresolved` with the count only — never as this method's amount, never
  as "free".
* Active rows of sibling options are surfaced as a count only (`ActiveSiblingOptionRuleCount`).
* Two equally specific owned rows (same scope + same condition) or an owned rule set with mixed
  currencies → `Conflicting`; no single amount is asserted.
* No coverage grant ⇒ `ActiveCoverageGrantCount = 0` and no universal "quote ready" claim.

## Focused proof

* `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingMethodFinancialAuthorityTests.cs`
  (14 tests, all green) — real `ShippingDbContext` + real `ShippingRateCoverageDirectory` evaluation:
  owned rule, `tipax:standard` vs `tipax:express`, `post:standard` vs `post:express`, legacy shared,
  optionless ownership, missing tariff ≠ free, configured-but-no-coverage, stored zero ≠ free mode,
  ambiguous duplicate scope, mixed currency, conditioned rule, deactivated rule, query validator,
  route ownership.
* `src/frontend/app/admin/shipping-services/shipping-method-financial-model.test.ts` — Admin mapping
  fail-closed behaviour (no ready/free/zero invention, sibling leakage impossible by shape).
