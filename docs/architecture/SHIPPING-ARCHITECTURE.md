# Tooba Shipping — Architecture Decisions (Draft v0.1)

Date: 2026-10-14  
Status: **PLANNING / PROPOSED**; no production implementation authorized.  
Prerequisite: `TB-SHIPPING-AUDIT-034` audit and Architect review before code changes.

## 0. Context and non-goals
Tooba is a multilingual, multi-seller marketplace. The current checkout shipping projection is reported to mix DB catalog availability, appsettings rates/lead days and hard-coded delivery slots. This document records target ownership and product decisions, **not claims of existing implementation**. Do not duplicate existing Fulfillment, Order or Shipping contracts without source audit.

## 1. Module ownership
- **Catalog / Product/SKU** owns physical characteristics (actual/dimensional weight, dimensions, shipping restrictions) and logistics category: NORMAL / LARGE_HEAVY / EXTRA_HEAVY. Sellers may not arbitrarily redefine the intrinsic product category.
- **Offer** owns price, inventory, supplying/dispatch origin where applicable, seller preparation promise and fulfillment responsibility or permitted model; avoid embedding shipping rate algorithms or repeating store-wide free-shipping rules in each Offer.
- **Shipping** is the proposed authority for service/option catalogs, carrier coverage, zones, rating policies, seller/Tooba shipping policies, quotes, availability and service-specific delivery calendars.
- **Order** owns a stable snapshot of the shipping option, quote/buyer charge, destination, promised time window, financial obligations and corresponding delivery groups at order placement.
- **Fulfillment** owns real operational preparation, physical packages/shipments, tracking, transitions and execution against the order promise.
- **Payment / Settlement** owns collection, allocations, payables, subsidies and settlements; must distinguish carrier payable from a generic shipping revenue bucket.
- Host composes modules only; cross-module interactions through approved Contracts/interfaces, consistent with Tooba's architecture guards.

## 2. Geographic scope and localization
- Shipping UI starts with **Country → City → Sub-city/Service Area**, with stable IDs, translations, optional polygons/postal-code rules and serviceability resolution.
- Persist a flexible geographic hierarchy in domain data rather than hard-code globally fixed country-specific levels. Provinces/states may be represented when required by address/coverage systems even if not shown in the primary three-level shipping selector.
- City/sub-city names can coincide (e.g. Iran → Bandar Abbas → Bandar Abbas area); identity uses IDs, never localized text.
- Validate origin and destination eligibility for each service/option. Shipping across borders needs carrier/customs/rates/currency policies as a later scope, not inferred from localization alone.

## 3. Services, pricing and payment model
Each available service/option has explicit carrier/provider, execution owner, allowed package/logistics categories, geographic coverage, payment terms (**prepaid / collect-on-delivery**) and a rate source:
- FixedRate
- ZoneRate
- WeightOrDimensionalRate
- CarrierApiRate
- ManualQuotation

No single global BasePrice or generic delivery-slot list is sufficient for all providers. Store commercial shipping configuration and tariffs behind Shipping authority with version/effective dates; infrastructural switches and credentials remain in configuration/secrets.

**Separate all financial concepts**:
- `CarrierCost`: carrier charge, known/estimated/settled with reconciliation status.
- `CustomerShippingCharge`: amount charged to the buyer, including explicit collect-on-delivery semantics.
- `ShippingSubsidy`: who funds customer discounts (Tooba, seller, or approved split).
- `PricingAuthority`: who supplies/controls quoted tariff.
- `CollectionParty`: entity collecting the shipping amount.
- `CostBearer`: ultimate economically responsible entity/entities.
- `FulfillmentOwner`: party responsible for execution.
Do not assume the receiver of funds is the rate setter, cost bearer or physical carrier. Seller settings must not impose subsidy or carrier debt on Tooba without Tooba authorization.

## 4. Policies and free shipping
A Shipping Policy evaluates contextual conditions such as:
- scope = Tooba-global or Seller (first release); per-Offer overrides are **deferred**;
- qualifying merchandise subtotal, service/option, logistics category, seller, area, limits, effective period;
- minimum/maximum cart amount, weight and item count, as applicable;
- discount cap and subsidy cost bearer;
- explicit precedence, combining rules, audit/versioning and deterministic evaluation.

**Example**: Seller Arman funds free *standard post* shipping for eligible normal products when qualifying seller merchandise subtotal exceeds IRR-equivalent of 3,000,000 toman. Buyer charge can be zero while carrier payable remains nonzero. Qualification basis is the **eligible seller delivery group** by default, pending review of grouping semantics; a platform policy for entire order is possible only by explicit design. Different shipping policies must not double-discount or create negative charges.

Seller-wide settings/policy association belong under Shipping/Seller Shipping Settings, **not copied into every Offer**.

## 5. Availability, delivery promise and grouping
- Quote should be computed for an eligible **delivery group**, not by a single max preparation duration across all sellers in the cart.
- Group by compatible customer order, seller/execution responsibility, dispatch origin, destination, chosen service/option, logistics constraints and promised delivery window. Group keys must not be based on service name alone.
- At Checkout, offer only services and windows that can genuinely be fulfilled (carrier calendars and operating capacity; no universal fabricated slot promise).
- Selection and financial/time commitments are snapshotted on order placement; subsequent catalog/rate edits must not silently alter buyer's commitment.
- **Delivery Group is a promise/operational grouping; Shipment is a physical package/dispatch.** A delivery group may have multiple shipments.
- Combined packaging is permitted only after compatibility checks (same customer/order ownership, service, origin, physical compatibility and executable time window). **Never physically consolidate different customers' items in the same customer shipment.**
- Bulk action across groups may process independent groups in a transactionally safe manner, but must not unify their identifiers, tracking or state transitions.

## 6. Timing, capacity and packing fee
- Model preparation lead time separately from carrier transit time; do not use global max-seller preparation to delay unrelated seller groups.
- Support configurable working days, service-specific time windows, blackouts, delivery horizons and per-window capacity where actually controllable.
- Support packing fee as a distinct rated component, tied to responsible owner and transparent to the buyer.
- For providers without a guaranteed appointment, display estimated range rather than a hard delivery appointment.

## 7. Admin and seller experience
- Tooba admin can govern shipping service catalogs, provider contracts, tariffs/approvals, region coverage, calendars, global subsidies and operational permissions.
- Seller can manage only authorized seller-owned shipping policies, dispatch readiness/origin and coverage; must not bypass global safety, cost or carrier constraints.
- Admin Fulfillment screen design direction: horizontal Seller tabs → vertical Due Today/Tomorrow/Future/Overdue tabs → groups by service and actual delivery slot → shipment/packing actions. This is **UX-only planning**; existing operational controls must be preserved through staged implementation (two presentation-only tasks; explicit Architect approval before wiring).

## 8. Staged delivery and verification gates
0. **Mandatory read-only audit** `TB-SHIPPING-AUDIT-034` of actual current behavior across Shipping, Catalog, Offer, Order, Fulfillment, Payment, Settlement.
1. Architect review of audit, resolve existing vs missing authorities and write focused ADRs/contract changes.
2. Establish authoritative geography/service/rate/policy/calendar models without duplicating modules.
3. Build deterministic group-level quoting and commitment checks.
4. Integrate Checkout/Order delivery snapshot, preserve Cart Active until final Pay action.
5. Add settlement/carrier payable and reconciliation semantics.
6. Implement UX-only Fulfillment layout in two approved tasks; wire only after visual approval and permission.
7. Expand API carrier connections, capacity, international shipping based on proven need.

## 9. Open decisions; not yet certified
- Exact shipping-module extraction boundary from existing Fulfillment/Order implementation.
- Province/state administrative metadata vs three-level customer-facing selector.
- Qualifying subtotal for global free-shipping and cross-delivery-group eligibility.
- Collect-on-delivery money flow and multi-party authorization / partial collection.
- Seller payout and carrier payable reconciliation, quoted vs actual freight adjustments.
- Shipping tax, currency/exchange, customs for cross-border operations.
- Policy precedence and how contradictory seller/carrier constraints fail closed.
- Physical consolidation rules for multiple groups in the **same** customer order.

**Guard:** This draft records architecture intent only. No migration, Host or production behavior change is authorized by this document. Facts from the source audit must be cited before promoting any statement about present implementation to certified status.
