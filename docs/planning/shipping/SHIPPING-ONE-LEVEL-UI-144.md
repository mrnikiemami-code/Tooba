# SHIPPING ONE-LEVEL UI — TB-SHIPPING-ONE-LEVEL-TABS-144

Task: `TB-SHIPPING-ONE-LEVEL-TABS-144` (Parent `TB-SHIPPING-ONE-LEVEL-TABS-143`)
Mode: `BOUNDED_FINAL_OPERATIONAL_ACCEPTANCE_AND_COMMERCIAL_POLICY_DECISION_GATE`
Base SHA: `5ff8b546512730effb2a72f21195051493f4c8e4` (HEAD == origin/main at claim)
Status: **PASS (technical delivery accepted at this checkpoint) — STOP_FOR_ARCHITECT**
Source changes: **NONE.** This checkpoint is a documentation/decision gate; see §6.

---

## 1. Mission A — final delivery review (acceptance matrix)

Read-only review of `SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md`, the locked
`SHIPPING-METHOD-GROUPING-UI-CONTRACT.md`, and the TB-136/137/138/140/143 results plus the current code
seams (§3). No completed work was reopened, no migration or browser run was repeated.

### 1.1 Genuinely runtime-proven (evidence exists, not inference)

| Acceptance | Status | Runtime proof (source of truth) |
| --- | --- | --- |
| Per-line shipping method selection on the real Shipping page (one seller, two lines, two different methods) | **PROVEN** | TB-136: real browser + isolated DB readback — 2 groups, 1 seller, 2 distinct methods, disjoint cart lines, fee sum 380,000; cold reload deterministic |
| Blank legacy `روش ارسال:` label on a multi-group draft (customer-visible defect) | **PROVEN FIXED** | TB-137: DOM readback on `/fa/payment` renders `روشهای ارسال` + one row per authoritative group; aggregate `هزینه ارسال` and `قابل پرداخت` still exactly once; RTL + 393×887 mobile checked; 0 CSS edits |
| Nothing committed merely by opening/continuing Checkout | **PROVEN** | TB-138: after the single Continue click, draft groups exist while `checkouts=0, seller_orders=0, payments=0, allocations=0` |
| Final submit produces one accepted Checkout, one SellerOrder, two confirmed shipping groups with disjoint line membership | **PROVEN** | TB-138 (`state=Confirmed`, group charge total 380,000) |
| Charge reconciled exactly once, with real stable `ShippingGroupId` allocation targets (not the legacy sentinel) | **PROVEN** | TB-138 payment allocation split; TB-140/143 re-read |
| Distinct states: initiated → paid → order accepted; single sandbox verification, no double payment | **PROVEN** | TB-138 (one payment row, one idempotency key, `provider=fake`) |
| Paid event actually reaches Fulfillment: one unit owning both stable group ids, item→line→group membership matching Order | **PROVEN** | TB-140: `fulfillments=1`, `shipping_groups=2`, `payment_inbox=1`, no `42703`, no handler failures; ids identical to order groups and payment allocations |
| One failing module handler no longer starves other handlers of the same event | **PROVEN** | TB-139 transport repair |
| Per-group Settlement charge ledger: two `PendingAllocation` rows keyed by the same two stable group ids, amounts equal to the paid allocations, all commercial columns `NULL`, no seller credit, no payout | **PROVEN** | TB-143: real storefront paid journey on an isolated SingleStore scratch with explicit capability opt-in; DB readback 200,000 + 180,000, `pending_rows=2`, `rows_with_commercial_value=0`, `payout_request_rows=0` |
| Deployment capability default stays OFF and cannot silently activate | **PROVEN** | TB-143 focused tests + `appsettings.json:29-33` (`Enabled: false`) |
| The gate cannot silently credit a non-marketplace sale or a missing-schema tenant | **PROVEN** (unit-level, plus one focused re-run in this task) | `SettlementChannelLedgerEligibilityTests` 8/8 — see §4 |
| No frontend/CSS/theme/token/RTL/admin surface was changed by the settlement work | **PROVEN** | TB-143 result; `git show --stat ca0f98982` contains no frontend file |

### 1.2 Remaining blockers (explicitly NOT claimed closed)

| # | Blocker | Type | Owner |
| --- | --- | --- | --- |
| B1 | Commercial allocation policy does not exist: carrier payable, signed seller/marketplace shares, discount funder, shipping tax, rounding, policy version/effective-date, payout timing | **BUSINESS DECISION** (hard gate) | Architect/business — see `docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md` |
| B2 | Production activation procedure undefined: the capability is OFF by default **and** the Settlement schema is not applied by startup auto-migration (Settlement registers zero `IModuleSchemaMigrator`; the explicit authority lives in the module migration registry, unreachable from startup) | OPERATIONAL | Architect |
| B3 | Refund/reversal of a group fee is not wired: `ReverseShippingGroupFeeAsync` has **no production caller** (tests only); a refunded or cancelled shipment cannot currently reverse its own paying group's allocation | TECHNICAL (blocked by B1) | Settlement, after B1 |
| B4 | No read/observability surface for the pending ledger: `ListShippingGroupFeesAsync` has no production consumer, so an operator cannot see pending group fees anywhere (admin or otherwise) | TECHNICAL (presentational only) | Settlement, can be scoped independently |
| B5 | `SettlementRefundSucceededHandler` does not consult the per-event gate (it only adjusts accrual and does not touch group fees today). Harmless now; it becomes a correctness question the moment B3 is implemented | TECHNICAL | Settlement, together with B3 |
| B6 | Edition=Marketplace deployments still cannot complete a real paid sale (the Marketplace edition migrates Settlement but holds no commerce schema/fixture) — TB-142 finding, unchanged by TB-143 | ARCHITECTURAL | Architect |
| B7 | Production payment path (real PSP `WebhookPaymentGateway` / `FailClosedPaymentGateway`), real transport (RabbitMQ), real auth (SMS/OTP provider) and production migration rollout are unexercised | PRODUCTION READINESS | Architect + Ops |

Nothing above is a defect introduced by this task, and no defect requiring a source edit was found (§8).

### 1.3 Critical practical edge (explicitly checked in this task)

* **Default is OFF and cannot be turned on accidentally.** `SettlementMarketplaceChannelLedgerPolicy`
  (`.../DependencyInjection/SettlementMarketplaceChannelLedgerOptions.cs:23-52`) registers handlers only for
  `Edition=Marketplace` or an explicitly-parsed `Tooba:Settlement:MarketplaceChannelLedger:Enabled=true`; the
  shipped Host value is `false` (`src/backend/Host/Tooba.Host/appsettings.json:29-33`). A malformed/absent
  value parses to `false`.
* **Explicit schema activation remains required.** `SettlementModule.AddServices` registers a
  `SettlementDbContext` and the module migration descriptor, but **no** `IModuleSchemaMigrator`
  (`SettlementModule.cs:74-83`); startup auto-migration therefore never creates `settlement.*`. A SingleStore
  deployment with the capability enabled but no schema gets the fail-closed `SchemaNotReady` path, not an
  exception.
* **Neither non-marketplace retail nor an unknown checkout can be credited.** The channel is read from the
  persisted `order.checkouts.channel` through the Order contracts seam; absent/unknown resolves to
  `SaleChannelNotMarketplace`, and the skip is counted (`tooba.settlement.ledger.skipped`) without throwing,
  so retries are not poisoned.
* **Legacy Marketplace behaviour is untouched**: the gate short-circuits to `Eligible` without even reading
  the channel or the schema, asserted by a test that also asserts the channel reader was never invoked.
* **Order/Fulfillment fan-out is unaffected** by a settlement skip.

---

## 2. Mission C — final release / handoff checklist

Hand this to the user for acceptance testing. Every line names what to do and what must be observed.

### 2.1 Pre-conditions (must be true before any test)

1. Test backend is a **disposable/isolated** deployment or the shared dev stack — never a production DB.
2. If testing the Settlement ledger at all: the deployment must have
   `Tooba:Settlement:MarketplaceChannelLedger:Enabled=true` **and** the Settlement schema explicitly applied
   to that database. Without both, no ledger row is expected — and that outcome is a correct skip, not a bug.
3. Payment provider is the Development sandbox (`Mode=Sandbox`, `DefaultProvider=fake`). No live PSP.
4. Note the exact base SHA under test before starting.

### 2.2 Customer-journey checklist

| # | Scenario | Steps | Expected |
| --- | --- | --- | --- |
| C1 | **Legacy single-line, single method** | one product, one method, Continue → Payment | the legacy `روش ارسال: <label>` line renders (not the grouped branch); one fee; payable correct |
| C2 | **One seller, two methods** | two products of the same seller, choose two different methods per line | one seller card, two line rows, two method choices; both survive a cold reload |
| C3 | **Two sellers, same method** | two sellers, same method code | stays **two** groups (never merged); two fees |
| C4 | **Two sellers, different methods** | as C3 with different methods | two groups, two distinct methods |
| C5 | **Mobile RTL** | 390–393 px viewport through Shipping and Payment | grouped layout renders, no horizontal overflow, RTL order correct, no CSS regression |
| C6 | **Shipping back-refresh** | choose methods → Continue → Payment → reload → back to Shipping | selected methods, date and window restored from persisted state; no silent revert to a cart-wide default |
| C7 | **No premature commit** | Continue only, then inspect | draft exists; `checkouts=0`, `seller_orders=0`, `payments=0` |
| C8 | **Final sandbox click side-effect timing** | press the final pay button, complete the sandbox page **once** | exactly one payment row, one idempotency key, one order; no double charge on a second click |
| C9 | **Order/Fulfillment stable groups** | after paying C2 | one fulfillment unit owning **both** stable group ids; item→line→group membership matches Order |
| C10 | **Fee sums** | compare across the Shipping card, Payment summary, Order groups and Payment allocations | identical per-group amounts and identical sum; each fee appears exactly once |
| C11 | **Pending Settlement, no payout** | read `settlement.shipping_group_fees` for the paid groups | one `Charge` row per paid group, `PendingAllocation`, commercial columns `NULL`, no seller credit containing shipping, zero payout requests |
| C12 | **Tenant/sale-channel safety** | pay on a non-marketplace (e.g. direct-retail) checkout with the capability enabled | no ledger row is written; no exception; skip counter increments |

### 2.3 Explicitly unexplored / to be verified before production (not covered by any test above)

* Real PSP (production gateway) — initiate, webhook callback, status query, failure/timeout handling.
* Real messaging transport (RabbitMQ) rather than the Development in-process configuration.
* Real authentication (SMS/OTP provider) and session/guest-merge behaviour on production infrastructure.
* Production migration rollout: who applies the Settlement schema, in what order, with what rollback.
* Per-deployment capability enablement audit (there is no central place that proves which deployments have
  the flag on).
* Multi-tenant isolation under concurrent tenants on one host.
* Shipment booking/dispatch/tracking per group (no automatic booking exists by design).
* Everything under B1/B3/B4/B5/B6/B7 in §1.2.

---

## 3. Code-owner seam inventory (where the policy lives and how it is invoked)

| Concern | Owner | Exact seam |
| --- | --- | --- |
| Registration (deployment capability) | Settlement | `SettlementMarketplaceChannelLedgerPolicy.ShouldRegisterHandlers` (`SettlementMarketplaceChannelLedgerOptions.cs:40`), consumed at `SettlementModule.cs:59` |
| Per-event eligibility (sale channel + schema readiness) | Settlement | `SettlementChannelLedgerEligibilityGate.EvaluateAsync` (`.../Gateways/SettlementChannelLedgerEligibilityGate.cs:42`) |
| Sale channel truth | Order | `IOrderCheckoutSalesChannelSettlementReader` → `OrderCheckoutSalesChannelSettlementBridge` reading persisted `order.checkouts.channel` |
| Schema readiness probe | Settlement | `SettlementLedgerSchemaReadiness` — scalar `to_regclass('settlement.shipping_group_fees')` (does not throw) |
| Fee recording (the only production invocation) | Settlement | `SettlementPaymentSucceededHandler.HandleAsync` → `SettlementDirectory.RecordShippingGroupFeesAsync` (`SettlementDirectory.ShippingGroupFees.cs:20`) |
| Charge source + reconciliation guard | Order/Payment via Contracts | `SettlementShippingGroupBridge.GetForPaymentAsync` (`:29`); mismatch ⇒ `ShippingGroupChargeMismatch` (`:65`, `:71`); zero charge ⇒ no row (`:58`) |
| Allocation policy (the missing piece) | Settlement | `ShippingGroupFeeAllocation` — `RecordAcceptedCharge` (`:110`) hard-codes every commercial column to `null`; `EnsurePayoutAllocationAvailable` (`:237`) throws `allocation_policy_missing`; `EnsureNotSellerRevenue` (`:230`) forbids seller-revenue reuse |
| Reversal (not wired to production) | Settlement | `ReverseShippingGroupFeeAsync` (`:79`) — no production caller |
| Read surface (absent) | Settlement | `ListShippingGroupFeesAsync` (`:111`) — no production consumer |
| Skip observability | Settlement | `tooba.settlement.ledger.skipped` (`SettlementInstrumentation.cs:25`, recorded at `:40`) |

---

## 4. Focused spot-check (exactly one guard, no broad suite)

Command and result (`evidence/focused-test-144.txt`):

```
dotnet test src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj -c Release
  --filter "FullyQualifiedName~SettlementChannelLedgerEligibilityTests"
=> Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 459 ms
```

That single class is the guard for the critical edge in §1.3: registration OFF by default, ON only for the
explicit capability, Marketplace registration not weakened, Marketplace legacy short-circuit without channel
reads, marketplace channel accepted, ordinary retail rejected, unknown checkout rejected, missing schema
fails closed.

---

## 5. Files and evidence

* `docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md` — Mission B decision record.
* `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-144.md` — this document (Mission A + Mission C).
* `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-144/` — `focused-test-144.txt`,
  `seam-inventory-144.txt`, `nonchange-and-isolation-144.txt`.

## 6. Non-change statement

No source file was modified. No frontend, CSS, theme, token, card markup, layout, RTL, responsive, admin or
shared setting was touched. No live PSP was used. Shared `:5088` / `:3000` and `tooba_alpha` were not used,
started, stopped or written. No new runtime was launched for this task (the one focused unit-test run uses an
in-memory provider). The settlement capability default remains OFF and no global migration order was changed.
No payout or commercial settlement was activated.

## 7. Decisions awaiting approval (the actual handoff)

1. Answer Q1–Q10 in the decision record (`SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md` §6) and sign the
   accounting equation (§4.1). Until then every group fee stays `PendingAllocation` and non-payable.
2. Decide the production activation procedure for the capability flag and the Settlement schema (B2), and
   whether B3/B4/B5 are approved as a follow-on bounded task.
3. Decide whether Edition=Marketplace must ever hold a runnable commerce fixture (B6), or whether the
   SingleStore + capability opt-in path is the intended production shape.
4. Decide production readiness scope for B7 (real PSP, transport, auth, migration rollout).

## 8. Defect search result

No source defect was found that blocks the main customer journey: the grouped selection, grouped payment
presentation, final submit, Order/Fulfillment group propagation and per-group pending ledger are all
runtime-proven. The remaining gaps are the deliberate fail-closed commercial gate (B1) and the operational /
observability items (B2–B7). Per the task instruction, nothing unapproved was changed inside this
decision task.
