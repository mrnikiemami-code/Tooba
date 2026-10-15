# TB-SHIPPING-FULL-OPERATIONS-076 — summary

Task: `TB-SHIPPING-FULL-OPERATIONS-076` · Parent: `TB-SHIPPING-FULL-OPERATIONS-075`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor
Mode: `BOUNDED_OPERATIONAL_QUOTE_AND_CAPACITY_AUTHORITY` · Stop-State: `STOP_FOR_ARCHITECT`
Expected-Base-SHA: `2c0b7e1adcd15c524ff3311334bd3df1a15e5b3b`
Bridge task row: `10842f5f-e08f-456b-907c-e27dede79aec`
Status: **PASS** (Worker PASS ≠ Architect ACCEPT)

## Deliverable

One additive, read-only, deterministic Shipping seam that composes facts Shipping already owns —
carrier freight, packaging fee, freight payer mode, weekly carrier operating schedule — into a single
auditable answer with an explicit payment split, plus a truthful statement about capacity.

Nothing else changed: no Checkout authority switch, no money movement, no capacity enforcement, no
schema/migration change, no frontend, no Order/Payment/Settlement/Host edit.

## Architecture (composition, not a second engine)

| Layer | Artifact | Responsibility |
| --- | --- | --- |
| Contracts | `Operations/ShippingOperationalQuoteContracts.cs` | `IShippingOperationalQuoteLookup`, `ShippingOperationalQuote`, request/evaluation DTOs, outcome/availability/capacity enums, reason codes |
| Domain | `Operations/ShippingOperationalQuoteEvaluator.cs` | Calls the existing rate engine + packaging resolver + schedule evaluator, then applies ordering and the payment split |
| Domain | `Scheduling/ShippingScheduleAvailabilityEvaluator.cs` | Pure ISO-weekday / half-open `[start, end)` window containment in the configured IANA zone, over an explicit instant |
| Application | `Operations/Queries/ShippingOperationalQuoteQueries.cs` | MediatR query + handler (`IRequest`, read-only) |
| Application | `Validation/ShippingOperationalValidators.cs` | FluentValidation rules and stable validation codes |
| Infrastructure | `Operations/ShippingOperationalQuoteLookup.cs` | Read-only EF Core reads of four module-owned sets; no foreign `DbContext`, no cross-schema join, no transaction |
| Endpoints | `Admin/ShippingOperationalQuoteEndpoints.cs` | `POST /v1/admin/shipping-services/{serviceCode}/operational-quote` behind the **existing** `fulfillment.view` permission |

`ShippingRatePolicyEngine.Quote` and `ShippingPackagingFeeResolver.Resolve` are invoked with exactly
the inputs the legacy seams receive, so option identity → destination depth → measurement tier
precedence stays owned by the rate engine and packaging precedence stays owned by the packaging
resolver. No pricing rule is re-implemented.

## Payment split (a quote fact, never a transaction)

| `FreightPayerMode` | Payable at checkout | Collect on delivery |
| --- | --- | --- |
| `Prepaid` | freight + packaging | `0` |
| `CollectOnDelivery` | packaging only | freight |

Packaging is never collectable: the carrier has no packaging contract, so only the freight component
can move to COD. `PackagingPayer` remains `Checkout` in both rows.

## Fail-closed money

Every money field is `decimal?`. `FreightNotQuoted`, `PackagingAmbiguous` and `CurrencyMismatch` all
leave the amounts `null` with a stable reason code — never a success-shaped zero. A free-freight
waiver applies to carrier freight only; the packaging component survives it.

## Schedule semantics — carrier availability, not a customer promise

`NotConfigured` and `InactiveNotEnforced` are fail-open (legacy methods stay open);
`Available` satisfies the constraint; `Unavailable`, `NotEvaluated` and `Ambiguous` do not.
The evaluator never reads the clock, so the same request always yields the same verdict. A guard test
proves no promise/estimate field exists on the schedule aggregate or the evaluation result.

## Capacity — configuration only, honestly labelled

No atomic idempotent shipment-slot reservation/cancellation exists and none was faked.
`ShippingCapacityEnforcement` has exactly one member, `NotEnforced`, so the type system prevents
reading the configured numeric cap as a real reservation. The configured total is still reported for
auditability with `shipping.capacity.not_enforced`.

**KNOWN PROGRAM GAP:** commitment/booking cannot rely on this numeric cap. Enforcing it needs a
separate, later-approved design with an atomic claim.

## Verification

* `dotnet test Tooba.Shipping.Tests` → **290 passed, 0 failed** (was 238; +52 new).
* Focused coverage: Prepaid vs COD split, free freight + nonzero packaging, ambiguous packaging,
  conflicting currency, unknown service, missing coverage, missing measurement vs unmatched tier,
  weight boundary 1999/2000, count boundary 5/6, both axes intersecting, option-specific precedence,
  destination-depth precedence, clock determinism, read-only seam shape, endpoint ownership.
* Live Development proof (positive identity `dev-singlestore` / `store-alpha` / `tenant-alpha` /
  `tooba_alpha` on `http://127.0.0.1:5088`): see `log-live-operational-quote.txt`, reproducible via
  `probe-operational-quote.ps1`. Sunday 13:30 Asia/Tehran inside the 13:00–14:00 window, 14:00
  outside (half-open end), 16:30 inside the second window, no instant → `NotEvaluated`, inactive
  schedule → `InactiveNotEnforced`, unknown option/country → declared 400, missing credential → 401,
  `GET` on the route → 405, `rates` body byte-identical before/after.
* No schema change: the seam reads only the existing `shipping.rate_policies`,
  `shipping.coverage_grants`, `shipping.packaging_policies`, `shipping.operating_schedules`.
