# SHIPPING-FULL-OPERATIONS-075 — packaging fee policy and freight payer contract

Task: `TB-SHIPPING-FULL-OPERATIONS-075` · Parent: `...-074` · Timebox: 45 minutes HARD

## Why packaging is separate from freight

`ShippingRatePolicy.ChargeAmount` answers "how much does the carrier charge to move this shipment".
A packaging fee answers a different question: "how much does preparing the parcel cost". They have
different shapes (freight has weight/count tiers and a free-above-threshold rule; packaging is a flat
operational fee), different owners of the decision, and different accounting. Folding packaging into
`chargeAmount` would have made the free-shipping threshold silently zero the packaging cost too, and
nothing would be auditable. This task therefore adds a **second, independent** Shipping-owned
commercial aggregate.

    10|## Data contract

New aggregate `ShippingPackagingPolicy` in schema `shipping`, table `shipping.packaging_policies`:

| Column | Meaning |
| --- | --- |
| `packaging_policy_id` | stable id (uuid, application-assigned) |
| `service_code` | Fulfillment service code — compatibility key only, no catalog row duplicated |
| `option_code` | real shipping option identity; `NULL` = service-level policy |
| `country_id` / `administrative_area_id` / `city_id` / `service_area_id` | Geography destination scope (stable ids, no cross-schema FK) |
| `fee_amount` | flat fee, `>= 0`, `numeric(18,2)` |
| `currency_code` | ISO 3-letter (e.g. `IRR`) |
| `freight_payer_mode` | stable int code: `0 = Prepaid`, `1 = CollectOnDelivery` |
| `is_active` | explicit active/inactive; rows are never physically deleted |
| `created_at` / `updated_at` | UTC |

Storage invariants mirror the aggregate exactly: non-negative fee, fee ceiling, canonical
service/option/currency codes, coherent scope (`service_area_id` requires `city_id`), a
`freight_payer_mode IN (0,1)` check so **no row can exist with no responsible paying party**, and a
`NULLS NOT DISTINCT` unique index over `(service_code, option_code, scope)`.

    30|## Precedence — deterministic, never first-row-wins

`ShippingPackagingFeeResolver` reuses the exact precedence order already documented and proven for
the rate engine, because both answer the same question shape over the same identity:

1. **Option identity first.** An option-specific policy wins over a service-level policy. If no
   option-specific policy matches, the service-level policy is the documented explicit fallback.
2. **Deepest destination scope wins.** Among same-identity policies, `ServiceArea > City >
   AdministrativeArea > Country`.
3. **Ambiguity fails closed.** If more than one active policy survives both steps, the result is
   `shipping.packaging.policy.ambiguous` and **no** fee is claimed. Choosing a row by row order would
   be a hidden commercial decision.

    40|## Zero default

No matching policy means the packaging fee is **zero** (`shipping.packaging.not_configured`), never a
hidden extra charge. A configured zero-fee policy is still reported as an explicit charged component,
so a caller can tell "configured zero" apart from "not configured".

## Free-shipping decision (explicit)

Carrier freight waived by its existing `FreeAboveThreshold` **does not** waive packaging. Free
shipping is a *freight* rule; packaging is an independent commercial fact. Nothing in this slice
couples them, and the combination test proves a `0` freight component still carries its packaging
component. A future configurable exception would need its own authorized rule, not an implicit side
effect.

    50|## Payer mode (Mission B) — PERSISTED

`ShippingFreightPayerMode` is a Shipping-owned Contracts enum with a stable database code:

- `Prepaid` — freight is payable at checkout by the customer. **This is the default**, so an omitted
  body value preserves legacy checkout behaviour exactly.
- `CollectOnDelivery` — freight is collected by the carrier/recipient at delivery.

Shipping states *who owes freight and when*; it is a quote fact, not a Payment transaction. No
Order/Checkout/Payment/Settlement file was touched and no money is collected by this module.

Packaging has its own payer enum (`ShippingPackagingChargePayer.Checkout`) with a single value: the
carrier never collects a packaging fee. The type exists so that adding a second party later is a
    60|contract change, not free-form prose in the database.

## Read-only composition contract (future-facing)

`IShippingShipmentCostLookup` returns `ShippingShipmentCost`: the untouched `ShippingQuote` carrier
freight component, the `ShippingPackagingFeeComponent`, the currency and the combined amount. It
**never** sums optimistically — an unpriced freight, mismatched component currencies, or ambiguous
packaging all leave `CombinedAmount` null with a stable reason code. It has **no live Checkout
consumer today**, so no extra charge is claimed anywhere in Checkout by this task.

## Admin HTTP surface

Four routes under the existing admin group and the existing `fulfillment.view`/`fulfillment.manage`
permissions — no new global privilege:

    70|```text
GET    /v1/admin/shipping-services/{serviceCode}/packaging-policies
POST   /v1/admin/shipping-services/{serviceCode}/packaging-policies
PUT    /v1/admin/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}
PATCH  /v1/admin/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}/active
```

The route service code stays authoritative over the body; scope and option identity are immutable
through update; validation covers negative/oversized fee, invalid currency, out-of-contract payer
mode, unknown service, unknown option, wrong geography, duplicate scope, cross-service id, and
missing credential (401).

## Migration and rollback

One additive migration `20261015071836_ShippingPackagingPolicy` creates the table, its checks and
    80|its two indexes. Nothing existing is altered or dropped. Rollback is `Down()` — drop
`shipping.packaging_policies` — and it is safe because no other table references it and no live
reader exists yet.

## Out of scope (deliberately)

No customer pricing cutover, no Order/Checkout/Payment/Settlement edit, no frontend change, no
carrier-cost/subsidy ledger, no commercial tariff invention.
