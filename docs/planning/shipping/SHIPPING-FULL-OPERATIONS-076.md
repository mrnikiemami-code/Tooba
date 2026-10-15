# SHIPPING-FULL-OPERATIONS-076 — operational quote, carrier availability and honest capacity

Task: `TB-SHIPPING-FULL-OPERATIONS-076` · Parent: `...-075` · Timebox: 45 minutes HARD

## What problem this slice solves

075 gave Shipping two independent commercial facts (carrier freight, packaging fee) and a persisted
payer mode, but nothing composed them. A caller that wanted "what does this shipment cost, who pays
what, and can the carrier even take it then" had to re-implement the precedence rules. This slice
adds **one read-only, deterministic composition seam** and explicitly refuses to do three things:

1. it does **not** switch Checkout price authority,
2. it does **not** move money,
3. it does **not** claim enforced capacity.

## Composition, not a second engine

`ShippingOperationalQuoteEvaluator` (Domain) calls the existing
`ShippingRatePolicyEngine.Quote` and `ShippingPackagingFeeResolver.Resolve` with exactly the inputs
the legacy seams receive, then adds ordering, the payment split and the schedule verdict. No pricing
rule is duplicated: option identity → destination depth → measurement tier stays owned by the rate
engine, and the packaging precedence stays owned by the packaging resolver.

`ShippingOperationalQuoteLookup` (Infrastructure) reads four narrow, module-owned sets — rate
policies, coverage grants, packaging policies and operating schedules — all filtered by service code
(+ country where the table has one). No foreign `DbContext`, no cross-schema join, no transaction.

## The payment split is a quote fact, not a transaction

| Payer mode | Payable at checkout | Collect on delivery |
| --- | --- | --- |
| `Prepaid` | freight + packaging | `0` |
| `CollectOnDelivery` | packaging only | freight |

Packaging is never collectable: the carrier has no packaging contract, so only the freight component
can move to COD. Shipping states *who owes what and when*; Order/Payment/Settlement keep exclusive
ownership of actual money movement. No such module was touched.

## Fail-closed money, never a success-shaped zero

Every money field on `ShippingOperationalQuote` is `decimal?`. `Outcome` distinguishes
`FreightNotQuoted`, `PackagingAmbiguous` and `CurrencyMismatch`; in all three cases the amounts stay
`null` and a stable reason code is returned. An unpriced freight, ambiguous packaging, or mismatched
component currencies are never summed optimistically. `IsOperationallyEligible` requires both a
quoted price **and** a satisfied schedule constraint.

## Schedule semantics — carrier availability, not a customer promise

`ShippingScheduleAvailabilityEvaluator` (Domain) is deterministic over an **explicit** instant: it
never reads the system clock, so the same request always produces the same verdict.

| Situation | Verdict | Constraint |
| --- | --- | --- |
| No schedule for the identity | `NotConfigured` | satisfied (legacy methods stay open) |
| Schedule exists but is inactive | `InactiveNotEnforced` | satisfied (documented safe meaning) |
| Active schedule, instant inside a window | `Available` | satisfied |
| Active schedule, instant outside every window | `Unavailable` | **not** satisfied |
| Active schedule, no instant supplied | `NotEvaluated` | **not** satisfied |
| Two active schedules for one identity | `Ambiguous` | **not** satisfied |
| Stored time zone unresolvable on this runtime | `Ambiguous` (`timezone_unresolvable`) | **not** satisfied |

Windows are ISO weekday + half-open `[start, end)` local time in the schedule's configured IANA zone.
An option-specific schedule for a *different* option is never applied; the service-level schedule is
the documented explicit fallback. Windows may not cross midnight (a Domain invariant), so "today or
tomorrow" never becomes ambiguous. The UTC→local conversion is total, so DST invalid/ambiguous local
times (which only arise converting local→UTC) cannot occur on this path.

This is explicitly **not** a promised delivery slot: the evaluator exposes carrier availability,
local weekday/time and a matched window id, and a guard test proves no promise/estimate field exists
on either the schedule aggregate or the evaluation result.

## Capacity — configuration only, and honestly labelled

No atomic, idempotent shipment-slot reservation/cancellation exists and none was faked.
`ShippingCapacityEnforcement` has exactly one value, `NotEnforced`, so the type system prevents a
caller from reading the numeric cap as a real reservation. The configured total is still reported
(for auditability) with the reason code `shipping.capacity.not_enforced`, and a guard test asserts
the enum can never grow a second value without an approved design.

**KNOWN PROGRAM GAP:** capacity commitment/booking cannot rely on this numeric cap. Enforcing it
requires a separate, later-approved design with an atomic claim; this task deliberately did not
simulate one.

## Read-only admin diagnostic surface

```text
POST   /v1/admin/shipping-services/{serviceCode}/operational-quote
```

One route, one authorization call, and the **existing** `fulfillment.view` permission — no new
capability. `POST` is used because the evaluation takes an explicit body (destination, measurement,
instant), not because anything is written. The route service code stays authoritative: the body
contract deliberately has no `ServiceCode` property. The query is modelled as an `IRequest`
(query) because it mutates nothing, and the handler validates method identity through the Fulfillment
Contracts seam and destination identity through the Geography Contracts seam before evaluating.

## Out of scope (deliberately)

No customer Checkout cutover, no Order/Payment/Settlement edit, no frontend, no schema/migration
change (the seam is a read-only composition of existing tables), no capacity enforcement, no
carrier tariff invention, no money movement.
