# TB-SHIPPING-FULL-OPERATIONS-075 — summary

Parent: TB-SHIPPING-FULL-OPERATIONS-074 · Channel: tooba-main · Worker: tooba-worker-01
Mode: `BOUNDED_PACKAGING_POLICY_AND_FREIGHT_PAYER_CONTRACT`
Expected-Base-SHA: `d2c9680f8b7b1fba2b5ca656b6a1334061142bcd` · Stop-State: `STOP_FOR_ARCHITECT`

## Overall status

- **Mission A (packaging policy, primary) — COMPLETE.** A Shipping-owned packaging fee policy is
  distinctly modeled, persisted, admin-authorable over real authorized HTTP, and proven read-after-write
  on Development. Packaging is **not** hidden inside `chargeAmount`.
- **Mission B (payer mode, secondary) — PERSISTED.** `freight_payer_mode` is a persisted stable code
  column with a documented `Prepaid` default that preserves legacy checkout behaviour.
- **Packaging quote integration — FUTURE-FACING.** `IShippingShipmentCostLookup` is a read-only
  composition contract with **no live Checkout consumer**; no extra charge is claimed in today's
  Checkout.

    10|## Ownership and data contract

| Concern | Owner |
| --- | --- |
| Packaging fee policy aggregate + payer enum semantics | `Tooba.Shipping.Domain` / `Tooba.Shipping.Contracts` |
| Deterministic precedence resolver (pure, no I/O) | `Tooba.Shipping.Domain/Packaging/ShippingPackagingFeeResolver.cs` |
| Admin CRUD + read-only seams | `Tooba.Shipping.Application` (CQRS/ISender) + `Tooba.Shipping.Infrastructure` |
| HTTP routes under `/v1/admin` | `Tooba.Shipping.Endpoints/Admin/ShippingPackagingPolicyEndpoints.cs` |
| Storage | `shipping.packaging_policies` (own schema, no cross-module FK) |

New table `shipping.packaging_policies`: `packaging_policy_id`, `service_code`, `option_code` (nullable =
    20|service-level), four-level destination scope, `fee_amount numeric(18,2) >= 0`, `currency_code` (3
letters), `freight_payer_mode` (`0 = Prepaid`, `1 = CollectOnDelivery`), `is_active`,
`created_at`/`updated_at`.

Storage mirrors the aggregate: fee non-negative + ceiling, canonical service/option/currency codes,
coherent scope, `freight_payer_mode IN (0,1)` so **no row can exist with no responsible paying party**,
and a `NULLS NOT DISTINCT` unique index over `(service_code, option_code, scope)`.

## Precedence (deterministic, never first-row-wins)

1. **Option identity first** — an option-specific policy wins over the service-level policy; the
   service-level policy is the documented fallback when no option-specific policy matches.
2. **Deepest destination scope wins** — `ServiceArea > City > AdministrativeArea > Country`.
3. **Ambiguity fails closed** — two equally specific active policies yield
    30|   `shipping.packaging.policy.ambiguous` and **zero** fee is claimed.

Zero default: no matching policy is `shipping.packaging.not_configured` and a fee of `0`, never a
hidden extra charge. A configured zero-fee policy is still an explicit charged component.

## Free-shipping behaviour (explicit decision)

Carrier freight waived by the existing `FreeAboveThreshold` **does not** waive packaging. Free
shipping is a freight rule; packaging is an independent commercial fact. Proven by
`A_free_freight_quote_does_not_remove_the_packaging_component` and
`A_free_freight_quote_keeps_the_packaging_component_in_the_combination`.

## Payer mode

`ShippingFreightPayerMode { Prepaid = 0, CollectOnDelivery = 1 }` — a stable enum, never free-form
    40|Persian text in the database. `Prepaid` is the default for an omitted value, so legacy checkout is
unchanged. Shipping states **who owes freight and when**; it creates no Payment transaction.
`ShippingPackagingChargePayer.Checkout` exists as a single-value contract so a carrier can never be
recorded as collecting a packaging fee.

## Admin HTTP evidence (Development `tooba_alpha`)

Target positively identified: `/__platform-commerce` → `edition=SingleStore`,
`deploymentId=dev-singlestore`, `tenantId=store-alpha`, `connectionReference=tenant-alpha`; Host
`http://127.0.0.1:5088`; database `tooba_alpha`.

| Step | Result |
| --- | --- |
| `POST /v1/auth/login` (dev bootstrap admin actor) | `200`, real Bearer session |
| `POST .../tipax/packaging-policies` 12,000 IRR `CollectOnDelivery` | `200`, id `01a13e7b-e198-7000-9753-f417791d20cb` |
    50| `GET .../tipax/packaging-policies` read-after-write | `200`, exact row returned |
| `PUT .../{id}` fee 15,000 + `Prepaid` | `200`, fee and payer mode changed, scope/option immutable |
| `PATCH .../{id}/active {false}` then `{true}` | `200` both, flag flipped, row preserved |
| negative fee / bad currency / out-of-contract payer mode | `400` with `shipping.validation.packaging_fee_negative` / `..._currency_invalid` / `..._freight_payer_mode_invalid` |
| unknown service / unknown option / invented country | `400` `shipping.service_code.unknown` / `shipping.service_option.unknown` / `shipping.destination_scope.invalid` |
| duplicate service/option/scope | `400` `shipping.packaging.policy.scope_duplicate`, no partial write |
| no credential | `401` `admin.actor.missing` |
| non-configured identity (`post`) | `200` empty set = zero fee |
| cleanup | task-owned fixtures deactivated |

    60|Fixture ledger (deactivated, never deleted — no delete surface exists):
`01a13e7b-e198-7000-9753-f417791d20cb` (tipax/standard/AdministrativeArea, 15,000 IRR) and
`01a13e7c-cfde-7000-98cc-f79246cf97b0` (tipax/express/AdministrativeArea, 15,000 IRR), both
`is_active=false`.

Preservation proof: `shipping.rate_policies` still has **11** rows (identical ids/amounts/conditions as
before this task), `coverage_grants` 7, `operating_schedules` 1. No existing row was altered or deleted.

## Migration and rollback

`20261015071836_ShippingPackagingPolicy` — purely additive: creates the table, its 7 check constraints
and its 2 indexes. Applied to Development through the canonical design-time context
(`TOOBA_DESIGN_TIME_CONNECTION`); migration history now shows 5 shipping migrations. Rollback is the
migration `Down()` (drop `shipping.packaging_policies`); safe because nothing references it and no live
    70|reader exists.

## Tests actually run

- `dotnet test Modules/Shipping/Tooba.Shipping.Tests` → **238 passed, 0 failed** (was 215; +23 new
  packaging tests across resolver, admin directory, shipment-cost composition and endpoint ownership).
- `dotnet build Host/Tooba.Host` → **0 warnings, 0 errors**.
- `dotnet build Tooba.Shipping.Tests` → 0 errors (1 pre-existing xUnit1031 warning in an untouched file).
- `dotnet ef database update` on the Shipping context → migration applied.

## Compatibility

- Existing rate/coverage/tier/schedule rows are untouched and their behaviour is unchanged.
- An omitted `freightPayerMode` persists as `Prepaid`, so legacy checkout semantics are preserved.
- The new packaging surface is additive: no existing route, contract or DI registration was replaced.
- No frontend change in this task (the existing dialog extension follows once API semantics are accepted).

## Remaining blockers / handoff

    80|- **Checkout/Order integration is not done.** `IShippingShipmentCostLookup` exists but nothing consumes
  it; wiring a packaging line into Order pricing is a separate Architect-reviewed task. Today's Checkout
  behaviour is deliberately unchanged.
- No delete surface for packaging policies; cleanup uses deactivation plus the id ledger above.
- No carrier-cost/subsidy ledger and no COD settlement is modeled here — payer mode is a quote fact only.
- Commercial Shipping/Checkout readiness is **not** claimed.
- `Worker PASS != Architect ACCEPT`.

## Artifacts

- `log-live-packaging-proof.txt` — full live authorized HTTP transcript.
- `probe-packaging.ps1` — the reproducible probe.
- `host-stdout-075.txt` / `host-stderr-075.txt` — Development host logs.
