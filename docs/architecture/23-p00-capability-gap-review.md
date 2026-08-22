# Tooba — P00 Capability Gap Closure Review

Status:

```text
P00 architecture gap review — candidate for Architect decision; not an ADR lock; not a Gate
```

Task:

```text
TB-P00-T024
```

Documentation only. No implementation, schemas, UI, Shopeiva import, or Gate issuance.

Cursor cannot authorize `P00-GATE`. This is an assessment only.

## A. Classification System

Each capability receives **exactly one**:

```text
DEDICATED_TASK_REQUIRED
BOUNDARY_SUFFICIENT_FOR_P00
DEFER_POST_P00
NEEDS_USER_PRODUCT_DECISION
```

Vague “later package / future work / deferred” without one of these labels is forbidden here.

## B. Review Method

Evaluated: first-sale need; business-truth ownership; money/accounting; state machines; SpiceDB; cross-module contracts; Order/Payment/Fulfillment/Pricing impact; public UX/SEO; Admin/Seller/Customer workflows; CRUD/cross-domain leakage if postponed; whether accepted P00 docs can host the capability later.

Shopeiva screens are **not** product requirements.

## C. Reviews / Ratings

**Architecture-Classification:** `DEDICATED_TASK_REQUIRED` (architecture delivered under TB-P00-T025; awaiting Architect ACCEPT)

**First-Sale-Criticality:** `SHOULD_HAVE_FIRST_SALE`

Reason: Decision-grade Reviews architecture is in `docs/architecture/24-reviews-ratings.md` (Product Review ≠ Seller Review; rebuildable rating projection; Order/Fulfillment contracts for verified purchase; published-only public/search/SEO/AI; Moderation Workspace + PDP composition). This gap row is not a Gate authorization. T025 Cursor PASS ≠ Architect ACCEPT.

## D. Notifications

**Architecture-Classification:** `BOUNDARY_SUFFICIENT_FOR_P00`

**First-Sale-Criticality:** `MUST_HAVE_FIRST_SALE`

Reason: First sale needs OTP and Order/Payment/Fulfillment notices, but accepted docs already require: domains emit intents/facts; Identity must not couple to an SMS vendor (`04`); Email/SMS/notification **provider adapters** (`19`); technical log != notification (`18`). Deeper template/preference/channel catalogs can follow without rewriting domains.

UX direction later: Template / Delivery / Preference Workspace — not domain CRUD of providers.

## E. Support

**Architecture-Classification:** `DEFER_POST_P00`

**First-Sale-Criticality:** `UNKNOWN_REQUIRES_USER`

Reason: T002 recorded template tickets as **not** a confirmed Tooba first-sale requirement. Architecture **can** host a later Support module without Gate if we forbid stuffing cases into Order notes/status. Do not promote Shopeiva tickets. If USER later confirms Support, it is a separate capability with SpiceDB and an Order **link by id**, not Order-owned tickets.

UX direction if confirmed later: Case Workspace.

## F. Returns / RMA

**Architecture-Classification:** `DEDICATED_TASK_REQUIRED` (architecture delivered under TB-P00-T026; awaiting Architect ACCEPT)

**First-Sale-Criticality:** `SHOULD_HAVE_FIRST_SALE`

Reason: Decision-grade Returns/RMA is in `docs/architecture/25-returns-rma.md` (Return ≠ Cancellation/Refund/Fulfillment/Inventory/Order; partial RMAs; Payment/Inventory instructions; reverse logistics adapter). This gap row is not a Gate authorization. T026 Cursor PASS ≠ Architect ACCEPT.

## G. Tax

**Architecture-Classification:** `DEDICATED_TASK_REQUIRED` (architecture delivered under TB-P00-T027; awaiting Architect ACCEPT)

**First-Sale-Criticality:** `MUST_HAVE_FIRST_SALE`

Reason: USER locked tax-exclusive base, Tooba-calculated configurable effective-dated percentage rules, Iran emphasis with UK/multi-market readiness, no hard-coded law. See `docs/architecture/26-tax-architecture.md`. T027 Cursor PASS ≠ Architect ACCEPT. B2B VAT/invoice remains out of initial phase.

UX direction later: Commercial/Tax Configuration Workspace after USER policy.

## H. Fraud / Risk

**Architecture-Classification:** `BOUNDARY_SUFFICIENT_FOR_P00`

**First-Sale-Criticality:** `SHOULD_HAVE_FIRST_SALE`

Reason: Must not conflate Fraud/Risk with Authorization, Authentication, PSP decline, or Analytics. Existing seams: Identity abuse/rate limits (`04`); Payment attempt/decline (`11`); Promotion coupon abuse (`22`); checkout fail-closed. A future `IRiskAssessment` adapter can attach without a P00 module. Leakage to watch: random fraud flags inside Checkout tables — forbid; keep optional policy/adapter at the application boundary.

UX direction later: Risk Review Queue for manual review, not Checkout CRUD.

## I. Cross-Capability Interaction Matrix

Cells: `OWNER_INTERACTION` | `CONSUMER` | `EVENT` | `CONTRACT` | `NONE` | `FUTURE`

| | Catalog | Party | Authorization | Pricing | Promotion | Cart/Checkout | Order | Payment | Inventory | Fulfillment | Content | Search | SEO | Analytics | Audit | Frontend UX |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Reviews | CONTRACT (product/offer id) | CONTRACT (author) | OWNER_INTERACTION (publish/moderate) | NONE | NONE | NONE | CONTRACT (verified purchase) | NONE | NONE | NONE | NONE | CONSUMER (projection) | CONSUMER (JSON-LD) | CONSUMER | EVENT | OWNER_INTERACTION (PDP/moderation UX) |
| Notifications | NONE | CONSUMER (locale/contact) | CONTRACT (who may send/see) | NONE | EVENT (promo notice) | EVENT | EVENT | EVENT | NONE | EVENT | CONTRACT (templates) | NONE | NONE | NONE | EVENT (delivery audit) | OWNER_INTERACTION (prefs UX) |
| Support | NONE | CONTRACT | OWNER_INTERACTION | NONE | NONE | NONE | CONTRACT (order link) **if confirmed** | NONE | NONE | FUTURE | NONE | NONE | NONE | NONE | EVENT | OWNER_INTERACTION (case UX) **if confirmed** |
| Returns | NONE | CONTRACT | OWNER_INTERACTION | NONE | FUTURE (restocking fee) | NONE | CONTRACT | CONTRACT (refund) | CONTRACT (restock) | CONTRACT (return shipment) | NONE | NONE | NONE | CONSUMER | EVENT | OWNER_INTERACTION (RMA UX) |
| Tax | NONE | FUTURE (B2B tax id) | CONTRACT (who configures) | OWNER_INTERACTION (quote policy) | CONTRACT (tax vs discount order) | CONSUMER | CONSUMER (snapshot) | CONSUMER | NONE | FUTURE (shipping tax) | NONE | NONE | NONE | CONSUMER | EVENT | OWNER_INTERACTION (config UX) |
| Fraud/Risk | FUTURE | CONTRACT (velocity) | NONE (not SpiceDB substitute) | NONE | CONTRACT (coupon abuse) | OWNER_INTERACTION (policy hook) | CONSUMER | OWNER_INTERACTION (PSP + risk adapter) | NONE | FUTURE | NONE | NONE | NONE | CONSUMER | EVENT | OWNER_INTERACTION (review queue) |

## J. First-Sale Criticality

| Capability | Criticality |
| --- | --- |
| Reviews / Ratings | SHOULD_HAVE_FIRST_SALE |
| Notifications | MUST_HAVE_FIRST_SALE |
| Support | UNKNOWN_REQUIRES_USER |
| Returns / RMA | SHOULD_HAVE_FIRST_SALE |
| Tax | MUST_HAVE_FIRST_SALE |
| Fraud / Risk | SHOULD_HAVE_FIRST_SALE |

Criticality ≠ architecture classification (a POST_FIRST_SALE capability can still be `DEDICATED_TASK_REQUIRED` if leakage is high; Returns is SHOULD_HAVE **and** dedicated).

## K. Architecture Leakage Test

| Capability | If not designed now, most likely leakage |
| --- | --- |
| Reviews | Rating/count columns and “stars” on Catalog Product; Search/SEO inventing aggregates |
| Notifications | SMS/email SDKs inside Identity/Order; OTP coupled to one vendor |
| Support | Tickets as Order notes/status; Shopeiva ticket screens treated as requirements |
| Returns | Return flags on Order; Fulfillment performing refunds/restock writes |
| Tax | Tax percent on Product or Promotion; mixed-currency “tax” scalars |
| Fraud/Risk | Boolean `isFraud` on Checkout; SpiceDB used as fraud engine |

Reviews and Returns: high leakage → dedicated tasks. Notifications/Fraud: seams exist if adapters stay at the edge. Support: leakage prevented by explicit DEFER + no Order stuffing. Tax: leakage prevented only after USER policy or a Tax task that encodes that policy.

## L. UI / UX Gap Test

Deferral must not imply future module CRUD.

| Capability | Workflow direction if implemented |
| --- | --- |
| Reviews | Moderation Workspace + PDP composition |
| Notifications | Template / Delivery / Preference Workspace |
| Support | Case Workspace (only if USER confirms) |
| Returns | Return Case / RMA Workflow |
| Tax | Commercial/Tax Configuration Workspace |
| Fraud/Risk | Risk Review Queue |

```text
Backend/module boundary != UI boundary
Build PASS != UI ACCEPT
Functional PASS != Visual ACCEPT
Desktop PASS != Mobile PASS
LTR PASS != RTL PASS
```

## M. Recommended Next-Step Sequence

Ordered; **do not execute**; **do not invent Task-IDs**:

```text
1. Dedicated Reviews / Ratings architecture task
2. Dedicated Returns / RMA architecture task
3. USER product decision on first-market Tax (inclusive vs exclusive, jurisdiction/scope)
   then, if USER confirms Tax as a first-sale engine, a dedicated Tax architecture task
4. P00-GATE
```

Not in this sequence (not `DEDICATED_TASK_REQUIRED`): Notifications, Fraud/Risk, Support (deferred with anti-leakage rule).

P00 Gate is **not** authorized by this document.

## N. Gate Readiness Verdict

```text
P00_GATE_NOT_READY_TASKS_REQUIRED
```

Reason: Reviews/Ratings and Returns/RMA still lack decision-grade ownership/lifecycle/contracts; skipping them would likely pollute Catalog/Order/Fulfillment/Payment. Tax is `NEEDS_USER_PRODUCT_DECISION` and also blocks a responsible Gate until USER policy exists or Architect accepts a documented default seam-only path after USER input. Notifications and Fraud/Risk have sufficient P00 seams. Support is deferred without promoting template tickets.

Cursor `P00_GATE_READY` is **not** used. Cursor cannot issue `BEGIN_TOOBA_CURSOR_GATE_V1`.
