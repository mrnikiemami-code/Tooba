# TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177 — Result

Parent: TB-SHIPPING-ADMIN-METHOD-PACKAGING-176
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_PACKAGING_CHARGE_OWNER_CONTRACT_AND_CUSTOMER_QUOTE_SEAM
Channel: tooba-main · Worker: tooba-worker-01 · Agent: cursor
Status: **PARTIAL** (authority unresolved — no customer charge implemented, none invented)
Stop-State: STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE

Base-SHA: `8de5f34e0c14977c8456b7f3d42140c0678b03f9` (HEAD == origin/main == Expected-Base-SHA at start)

## 1. Answer to the mission question

> Is the saved per-method packaging/freight amount a customer-payable component, a carrier/operating cost, or
> only operational metadata?

At the base SHA it is **operational metadata with a single, unimplemented `Checkout` payer intent** — not a
customer-payable component. Three independent facts establish this, and none of them is an assumption:

1. **No consumption path.** The only production reader of packaging is
   `ShippingPackagingFeeResolver` (`Shipping.Domain/Packaging/ShippingPackagingFeeResolver.cs:37`), reached
   through `ShippingPackagingFeeLookup` and the read-only `ShippingOperationalQuoteEvaluator`. No Order, cart,
   draft, payment, or settlement code references packaging.
2. **The one consumer refuses it on purpose.** `StorefrontShippingOperationalShadowComparer` returns
   `PackagingNotReflected` for any `packaging > 0m`, with the explicit rule that packaging "must not be
   silently folded into the customer's checkout amount".
3. **The customer quote has no packaging input at all.** `StorefrontShippingOwnedRateResolver` calls
   `IShippingRateQuoteLookup` → `ShippingRatePolicyEngine.Quote`, whose request/policies contain zero
   packaging concepts. The accepted per-group charge and `payment.allocations` therefore cannot contain it.

Full edge-by-edge matrix: `OWNER-AUTHORITY-MATRIX.md` in this folder.

## 2. Why implementation was not authorized (contract gate)

The task requires **all** of: currency + taxes + payer + explicit precedence + method-level option identity +
an approved consumer path. At the base SHA:

| Requirement | State |
| --- | --- |
| Currency, precedence, method-level `(ServiceCode, OptionCode)` identity, ambiguity fail-closed | present |
| Payer | only `ShippingPackagingChargePayer.Checkout`; no seller/platform/carrier variant, no approval of precedence over freight payer mode |
| **Taxes** | **absent** — no tax concept exists in `Contracts/Packaging` |
| **Approved consumer path** | **absent** — and the existing comparer explicitly forbids folding it in |
| **Fee-allocation authority** | **open** — `SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md` is `OPEN — AWAITING BUSINESS/ARCHITECT APPROVAL` (D7 tax, D8 rounding, D9/D10 policy version/window unanswered) |

Per the task: *"Any missing authority or undecided pricing responsibility => DO NOT implement or enable customer
charging."* That branch was taken deliberately. Adding packaging to any customer amount here would have been an
invented price.

## 3. Single smallest owner decision (blocking)

> Decide whether a method's persisted packaging fee is **a separately customer-payable surcharge on top of the
> freight charge** — and if so state (a) its tax treatment/remitting authority and (b) whether it is included
> in, or additive to, the accepted per-group `customer_charge_amount` — or declare it a **seller/platform
> operating cost that is never charged to the customer**.

That one answer determines whether the next implementation is a customer-quote seam or a cost-allocation seam.
Nothing else needs deciding first.

## 4. What was delivered (no production code change)

Two executable owner-boundary guard suites, so no later task can quietly turn packaging into a customer charge:

* `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Packaging/ShippingPackagingCustomerChargeBoundaryTests.cs`
  * customer rate engine / `ShippingQuoteRequest` / `Contracts/Rates/*` carry no packaging concept;
  * `ShippingPackagingFeeResolver` has exactly two production repliers (`ShippingOperationalQuoteEvaluator`,
    `ShippingPackagingFeeLookup`) — no cart/draft/payment entry;
  * the operational-quote surface is `View`-authorized, read-only, and has no write command;
  * `ShippingPackagingChargePayer` still has only `Checkout`, and packaging contracts still have no
    tax/settlement/payout concept.
* `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/StorefrontShippingPackagingChargeBoundaryTests.cs`
  * packaging is confined to exactly three operational-shadow files — no cart/draft/payment file;
  * the customer charge resolver uses `IShippingRateQuoteLookup` and never
    `IShippingOperationalQuoteLookup` / `IShippingPackagingFeeLookup` / `IShippingShipmentCostLookup`;
  * the operational-quote lookup has exactly one Order consumer plus DI composition;
  * the shadow gate is double-flagged and off by absence (no `= true` default);
  * a charged packaging amount is still reported as `PackagingNotReflected`.

TB-176 admin behaviour is untouched: no admin file, route, DTO, dialog, tab, or CSS was modified. Create/Edit/
View read-only controls, server-derived method binding, `(serviceCode, optionCode)` scoping, and legacy shared-row
quarantine are exactly as proven in TB-176.

## 5. Focused validation (exact commands in `test-commands.txt`)

| Suite | Result |
| --- | --- |
| New Shipping boundary guards | 5 / 5 passed |
| New Order boundary guards | 5 / 5 passed |
| Existing Shipping packaging + operational-quote suites (excluding the pre-existing guard) | 79 / 79 passed |
| Existing Order storefront shipping shadow/rate suites | 44 / 44 passed |
| Shipping structure/composition guards | 12 / 12 passed |
| Order storefront/application/manifest structure guards | 18 / 18 passed |

**Pre-existing failure, recorded honestly and not silenced:**
`OperationalQuoteCompatibilityTests.No_module_outside_shipping_references_the_operational_quote_types` fails at
the base SHA because the Order operational-shadow observer of an earlier approved slice references
`IShippingOperationalQuoteLookup` (plus its DI registration in `OrderModule.cs`). Proven pre-existing with
`git grep -l "IShippingOperationalQuoteLookup" HEAD -- src/backend`. The guard was neither rebaselined, narrowed
nor deleted. The Architect should decide whether that guard or that seam needs adjustment — this is outside
TB-177's authority.

## 6. Non-goals respected

No settlement allocation policy, carrier payable, fee share, tax assumption, checkout migration, provider quote,
carrier booking, financial-mode invention, or implicit payout. No schema change, no migration, no seed. No
shared-service writes, no scratch host, no `.next` purge. No reset/rebase/stash/force push.

## 7. Unverified

* Customer-charge end-to-end proof — intentionally not attempted, because no authorized seam exists.
* No HTTP or screenshot assertion is claimed; none was attempted, so nothing is dressed up as verified.
