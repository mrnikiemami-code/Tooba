# TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177 — Owner authority matrix (source-backed)

Base: `8de5f34e0c14977c8456b7f3d42140c0678b03f9` (`main` == `origin/main` at task start).
Scope: read-only trace of the persisted per-method packaging policy through every monetary surface in the
repository. No production source was changed by this task. Every row below is a repository fact at the base
SHA, not an intention.

## 1. The chain, edge by edge

| # | Edge | Reaches the customer payable? | Exact symbol / contract | Monetary semantics |
| --- | --- | --- | --- | --- |
| E1 | Persisted policy → packaging fee resolution | n/a (data only) | `ShippingPackagingPolicy` rows; `ShippingPackagingFeeResolver.Resolve` — `Modules/Shipping/Tooba.Shipping.Domain/Packaging/ShippingPackagingFeeResolver.cs:37` | `FeeAmount` + `CurrencyCode` per `(ServiceCode, OptionCode, Scope)`. `IsCharged` is a distinct fact from `FeeAmount`. Stored `0` is "no packaging fee", never "free shipping". |
| E2 | Packaging fee → read seam for other modules | NO (no consumer) | `ShippingPackagingFeeLookup : IShippingPackagingFeeLookup` — `Infrastructure/Packaging/ShippingPackagingFeeLookup.cs:17,44` | Read-only, fail-closed, never mutates. Resolves ambiguity to a typed reason code instead of a number. |
| E3 | Packaging fee → composite shipment-cost seam | NO (no consumer) | `ShippingShipmentCostLookup : IShippingShipmentCostLookup` — `Infrastructure/Packaging/ShippingShipmentCostLookup.cs:26-28` | Adds freight + packaging components but only as a *cost view*; currency mismatch and packaging ambiguity fail closed. Referenced only from DI (`ShippingModule.cs:63`) and its own tests. |
| E4 | Packaging fee → operational quote | n/a (admin/internal surface) | `ShippingOperationalQuoteEvaluator` — `Domain/Operations/ShippingOperationalQuoteEvaluator.cs:35,69` | `total = freightAmount + packagingAmount`; `checkout = total − collectOnDelivery`; CollectOnDelivery moves **freight only** because the carrier holds no packaging contract. Money fields are `decimal?` so "not quoted" cannot be `0`. |
| E5 | Operational quote → consumer | **NO for customer charge** | `StorefrontShippingOperationalShadowObserver` — `Modules/Order/.../OperationalShadow/StorefrontShippingOperationalShadowObserver.cs:51,193` | Only Order consumer. Gated by `Enabled` **and** `PrerequisitesConfirmed`, both default `false` (`StorefrontShippingOperationalShadowOptions.cs:28,34`). Observation/telemetry only: never writes quote, delivery promise, draft or order snapshot. |
| E6 | Operational quote → customer amount | **NO — explicitly refused** | `StorefrontShippingOperationalShadowComparer` — `.../StorefrontShippingOperationalShadowComparer.cs:130-135` | A positive packaging amount yields `StorefrontShippingOperationalShadowOutcome.PackagingNotReflected`; the comment states it "must not be silently folded into the customer's checkout amount". This is a deliberate recorded gap, not a charge. |
| E7 | Customer shipping quote (storefront) | YES — but **freight/rate only** | `StorefrontShippingOwnedRateResolver` → `IShippingRateQuoteLookup.QuoteAsync` → `ShippingRatePolicyEngine.Quote` — `Order/.../OwnedRate/StorefrontShippingOwnedRateResolver.cs:96-97`; `Shipping/Tooba.Shipping.Infrastructure/Rates/ShippingRateQuoteLookup.cs:23` | Input is `ShippingQuoteRequest` (service, option, destination, subtotal, currency, parcel) + rate policies/coverage/method declarations. `ShippingRatePolicyEngine` and `Contracts/Rates/*` contain **zero** packaging concepts. |
| E8 | Customer quote → cart group / accepted draft | YES (accepted snapshot) | Order accepted shipping-group snapshot feeds `SettlementShippingGroupBridge.GetForPaymentAsync` — `Modules/Settlement/.../Bridges/SettlementShippingGroupBridge.cs:29,65,71,82` | The accepted per-group charge must equal the customer's paid group allocation; mismatch fails closed with `ShippingGroupChargeMismatch`. Because E7 has no packaging input, this amount cannot contain packaging today. |
| E9 | Payment preview / allocation | YES (charge) / NO (packaging) | `PaymentAllocationTargetKind.StoreShippingGroup` — `Modules/Payment/Tooba.Payment.Domain/ValueObjects/PaymentAllocationTargetKind.cs:20` | One allocation per stable `ShippingGroupId`; the sum is charged exactly once. No packaging component, no invented split. |
| E10 | Settlement ledger / payout | NO (fail-closed) | `ShippingGroupFeeAllocation.EnsurePayoutAllocationAvailable`, `EnsureNotSellerRevenue`; policy state `PendingAllocation` only | Every commercial column (`carrier_payable`, `seller_share`, `marketplace_share`, `shipping_tax`, policy version/reference) is `NULL`/`UNSPECIFIED`. `docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md` is **OPEN — AWAITING BUSINESS/ARCHITECT APPROVAL** and decides nothing. |

## 2. What the four money concepts actually mean here

| Concept | Where it lives today | Is it the customer's payable amount? |
| --- | --- | --- |
| **Packaging service/fee amount** (`ShippingPackagingPolicy.FeeAmount`) | Shipping domain, read via `ShippingPackagingFeeResolver` | **NO.** Persisted operational policy. It has a currency and a single `Checkout` payer intent, but no consumption path in any customer charge. |
| **Customer shipping charge** (per stable group) | Order accepted quote → `ShippingGroupFeeAllocation.CustomerChargeAmount` → `payment.allocations` | **YES**, and it equals the rate-quote (freight) amount only. Packaging is not an input. |
| **Amount payable on delivery** (`AmountCollectOnDelivery`) | Operational quote only (default-off shadow); `ShippingFreightPayerMode.CollectOnDelivery` | Not a customer quote today. Packaging never moves to COD: the carrier has no packaging contract. |
| **Seller / marketplace / carrier settlement** | Settlement ledger, all columns `NULL`, state `PendingAllocation` | Undecided. Blocked by an open business decision, and forbidden to infer. |

## 3. Does an already-approved contract authorize a separately customer-payable packaging charge?

Required by the task: correct currency **and** taxes **and** payer **and** explicit precedence **and**
method-level option identity — **all** of them, plus a permitted consumer path.

| Requirement | Present at base SHA? | Evidence |
| --- | --- | --- |
| Currency | YES | `ShippingPackagingPolicy.FeeAmount` + `CurrencyCode`; `CurrencyMismatch` fail-closed in both the operational quote and the shipment-cost seam. |
| Payer | PARTIAL | `ShippingPackagingChargePayer` has exactly one member, `Checkout = 0` (`Contracts/Packaging/ShippingPackagingContracts.cs:41-46`). There is no seller/platform/carrier variant, and no decision record approving `Checkout` precedence over freight payer mode. |
| Explicit precedence (option > service, city > area) | YES | `ShippingPackagingFeeResolver` resolves scope precedence; ambiguity yields `ShippingPackagingReasonCodes.Ambiguous` and the quote fails closed. |
| Method-level option identity | YES | `(ServiceCode, OptionCode)` bound from the server route; `OptionCode = null` is the quarantined legacy service-level row, never inherited by a method. |
| Taxes | **NO** | No tax concept exists anywhere in `Contracts/Packaging` (guard asserts `Tax`/`Settlement`/`Payout` absent). The locked `SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md` addendum defines financial modes for the *shipping charge*, not for a packaging surcharge. |
| Approved consumer path | **NO** | No Order/cart/draft/payment code reads packaging (only the default-off observer). `StorefrontShippingOperationalShadowComparer` explicitly refuses to fold it in. |
| Fee-allocation authority for the resulting charge | **NO** | `SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md` is open; D7 (shipping tax), D8 (rounding), D9/D10 (policy version/window) unanswered. |

**Conclusion: not all conditions hold. No customer-charge implementation is authorized.**

## 4. The single smallest owner decision that unblocks this

> Decide whether a method's persisted packaging fee is **a separately customer-payable surcharge on top of
> the freight charge**, and if yes, state (a) its tax treatment/remittance authority and (b) whether it is
> included in, or additive to, the accepted per-group `customer_charge_amount` — or instead declare it a
> **seller/platform operating cost that is never charged to the customer**.

Answering only that question determines whether the correct next implementation is a customer-quote seam or
a cost-allocation seam. Until it is answered, adding packaging to any customer amount would be an invented
price, which this task is explicitly forbidden to create.
