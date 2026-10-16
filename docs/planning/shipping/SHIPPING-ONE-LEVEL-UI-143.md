# SHIPPING-ONE-LEVEL-UI-143 — Bounded Settlement sales-capability activation and pending-ledger proof

Program: `TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE`
Parent: `TB-SHIPPING-ONE-LEVEL-TABS-142`
Mode: `BOUNDED_SETTLEMENT_SALES_CAPABILITY_ACTIVATION_AND_PENDING_LEDGER_PROOF`
Verdict: **PASS** for Mission A (implementation), Mission B (runtime proof) and Mission C (tests/evidence).

## 1. The problem the Architect authorised solving

TB-142 proved the two halves of the checkout/settlement boundary were disconnected:

* `Edition=Marketplace` migrates the Settlement schema but holds no runnable commerce fixture.
* The sanctioned `Edition=SingleStore` scratch has real Marketplace-channel offers, tax, Payment and
  Fulfillment but registered no Settlement handler at all (`SettlementModule` gated the handlers on
  `Edition == Marketplace`).

The ruling forbade conflating **edition** with **sales channel** and forbade an Edition swap. It
authorised a *minimal, capability-based* handler activation for a deployment with a verifiably
applicable **Marketplace-channel paid sale** AND an **explicitly available Settlement persistence
schema**, while retaining Marketplace handler availability and SingleStore legacy semantics.

## 2. Mission A — smallest safe handler eligibility (implemented)

Two distinct facts are now modelled separately, which is exactly the distinction the ruling demanded.

### 2.1 Registration eligibility = deployment capability (owner: `SettlementModule`)

`SettlementMarketplaceChannelLedgerOptions` (`SectionName = Tooba:Settlement:MarketplaceChannelLedger`,
default `Enabled = false`) plus `SettlementMarketplaceChannelLedgerPolicy`:

* `ShouldRegisterHandlers = IsMarketplaceEdition || IsLedgerCapabilityEnabled`
* `RequiresMarketplaceChannelOnly = !IsMarketplaceEdition && IsLedgerCapabilityEnabled`

Consequences, verified by focused tests:

* `Edition=Marketplace` → handlers registered, unchanged (availability retained).
* `Edition=SingleStore`, capability absent/false → **no** handler registration, unchanged legacy
  semantics, and therefore no possibility of a missing `settlement` relation poisoning ordinary
  SingleStore payment events.
* `Edition=SingleStore`, capability explicitly `true` → handlers registered (the authorised opt-in).

No canonical migration order constant was added and Settlement was **not** added to the startup
migration chain, so shared Development tenants are never given Settlement DDL by this change.

### 2.2 Per-event eligibility = sale channel + schema readiness (owner: `Settlement`)

* `IOrderCheckoutSalesChannelSettlementReader` (new, `Tooba.Order.Contracts.Settlement`) exposes
  `OrderCheckoutSalesChannelSnapshot(CheckoutId, Channel)` and is implemented by
  `OrderCheckoutSalesChannelSettlementBridge` (Order.Infrastructure) reading the **persisted**
  `order.checkouts.channel` through `OrderDbContext`. The channel is Order-owned durable state; it is
  never derived from a frontend method label, seller name, SKU, tenant edition guess or client claim.
* `ISettlementChannelLedgerEligibilityGate` / `SettlementChannelLedgerEligibility`
  (`NotMarketplaceChannel | SchemaNotReady | Eligible`) decides per event.
* `ISettlementLedgerSchemaReadiness` / `SettlementLedgerSchemaReadiness` answers "is the ledger
  relation present?" with a single scalar `to_regclass('settlement.shipping_group_fees') is not null`
  probe, which **does not throw** for an absent schema/relation.

`SettlementPaymentSucceededHandler` therefore evaluates the gate **before** any Settlement query and
returns without an exception when the decision is not `Eligible`; a non-applicable or not-ready event
is never surfaced as a transport failure, so retries are not poisoned and no payout is produced. A
`tooba.settlement.ledger.skipped` counter records the fail-closed skip.

Ordering inside the gate: legacy `Edition=Marketplace` short-circuits to `Eligible` **without** even
reading the channel or the schema, so existing Marketplace behaviour is bit-for-bit retained (asserted
by `Marketplace_edition_keeps_legacy_per_event_behaviour_without_any_channel_or_schema_work`, which
also asserts the channel reader was never invoked).

## 3. Mission B — runtime proof on a disposable SingleStore scratch (EXECUTED)

Environment (task-owned and disposable): container `tooba-tb143-pg` (`postgres:16-alpine`,
`127.0.0.1:55463`, databases `tooba_143_scratch` / `tooba_143_messaging`, role `scratch143`), isolated
Host published to `.tmp-143-host-out` (SHA below) launched exactly **once** on `http://127.0.0.1:5411`,
single Active tenant `store-alpha → tenant-143-scratch` whose `StoreCommerce.SalesChannel` is
`Marketplace`.

`prepare-isolated-target-143.js` printed `PREPARE=PASS` with `failClosed=true`: one tenant, one Active
tenant, canonical gateway tenant present, no `tooba_alpha`, no `tenant-alpha`, no `Port=5432`, no prior
scratch port, no `ShippingRateAuthority`, no `ShippingOperationalShadow`, `edition=SingleStore`,
`editionSwapPerformed=false`, `settlementCapabilityExplicitlyEnabled=true`.

Settlement schema activation reused the **proven TB-141 seam** (the module's own
`ToobaNpgsql.ConfigureModuleContext` path) against the task-owned scratch only; raw output in
`settlement-migration-activation-143.txt`, idempotent second `MigrateAsync` = no-op.

The customer journey is the real, API-driven storefront flow (`journey-143.js`, derived from the
accepted TB-140 journey): OTP-authenticated customer, guest cart merged into the authenticated cart,
two sibling offers of **one** seller, shipping projection, `PUT /shipping/selection` with **two**
accepted groups, one final click (`start-payment`) and one Development sandbox verification. No event
injection, no direct handler or `SettlementDirectory` call.

### Read-back (`scratch-readback-143.txt`) — the invariant under proof

| Invariant | Observed |
| --- | --- |
| ONE succeeded Payment | `01a143f3-33ac-…`, `3977000.0000 IRR`, `Succeeded` |
| TWO StoreShippingGroup allocations | `200000` + `180000` IRR, `target_kind=StoreShippingGroup` |
| ONE paid SellerOrder | `paid_seller_orders = 1` |
| ONE FulfillmentUnit with the TWO group ids | `fulfillment_unit_rows = 1`, `distinct_fulfillment_ids = 1`, 2 `fulfillment.shipping_groups` rows |
| Identical group identities across Fulfillment and Settlement | `identical_group_ids = 2` |
| TWO `settlement.shipping_group_fees` charge rows | `charge_rows = 2`, `distinct_groups = 2`, `charge_kind_rows = 2`, `reversal_kind_rows = 0` |
| Customer charges 200000 / 180000 | `200000.0000` (`post:express`) and `180000.0000` (`tipax:standard`) |
| `allocation_state = PendingAllocation` | `pending_rows = 2` |
| Unknown commercial fields NULL | `rows_with_commercial_value = 0` (carrier/seller/marketplace share, discount, tax, funder adjustment/reference, allocation policy version/reference) |
| Charge equals the customer's paid group allocation (†) | ledger charge = paid allocation for both groups |
| ZERO shipping amount in seller merchandise accrual | single accrual `gross=3597000.0000` = the SellerOrder allocation only; no 200000/180000 credit |
| No payout | `payout_request_rows = 0` |
| Settlement provenance (††) | `settlement.__ef_migrations_history = [InitialSettlement, AddSettlementShippingGroupFees]` |
| Duplicate real delivery (†††) | `groups_with_more_than_one_charge_row = 0` |

**Capture completeness (declared, not hidden).** The single psql capture persisted as
`scratch-readback-143.txt` ends at line 70: the rest of section E (`seller_credit_rows_carrying_a_group_charge`,
`accrual_total_minus_both_shipping_charges`) and the whole of the SQL file's sections F/G/H would have followed, but
the shell capture was cut at that point and the disposable database has since been destroyed, so the tail cannot be
re-fetched. Consequences, so nothing below is read as raw persisted output:
(†) `Charge equals the paid group allocation` is directly readable from the persisted capture itself — its section B
allocation rows and section D ledger rows carry the same two group ids with the same amounts
(`…a5ac-f125e2e761f2` = 180000, `…b59d-f3c4e951ad2e` = 200000);
(††) `Settlement provenance` is evidenced by the persisted `settlement-migration-activation-143.txt`;
(†††) `Duplicate real delivery` follows from the persisted section D counts
(`charge_rows = 2`, `distinct_groups = 2`, `charge_kind_rows = 2` → no group can hold more than one charge row).
No value above was invented and this note changes no verdict.

Authoritative eligibility source visible in the same read-back: `order.checkouts.channel = Marketplace`.

Nothing was fabricated: every row above was written by the production event path during the real paid
journey.

## 4. Mission C — tests and evidence

Focused, once, no rerun loop (`focused-tests-143.txt`):

* `Tooba.Settlement.Tests` — **47 passed / 0 failed**, including the seven new
  `SettlementChannelLedgerEligibilityTests`: SingleStore registration off by default, on only for the
  explicit capability, Marketplace registration not weakened, Marketplace legacy per-event short-circuit
  (channel reader never invoked), Marketplace-channel sale accepted, ordinary retail channel rejected,
  unknown checkout rejected, schema-not-activated fail-closed.
* `Tooba.Host.Tests` — **7 passed / 0 failed, 0 skipped**, including the pre-existing
  `Marketplace_registers_settlement_payment_and_refund_handlers_singlestore_does_not` guard (still
  green, unchanged) and the Docker-backed
  `Marketplace_handlers_accrue_then_adjust_idempotently_without_direct_settlement_api` end-to-end
  Marketplace consumer path, which now exercises the injected gate through the legacy short-circuit.

## 5. Release/safety

No percentage, discount, funder or carrier policy was invented. No `PendingAllocation` row was ever
presented as settled, revenue, credit, or payable. No schema activation occurred on any shared database:
the shared `:5088` / `:3000`, `tooba_alpha`, the shared Postgres on `:5432` and the shared RabbitMQ were
never touched. No frontend, CSS, theme, typography, card, spacing, RTL, component-token or admin file
was modified. No automatic shipment booking was added. The already-proven paid → Fulfillment group
handoff is untouched and re-read as still correct in section A of the read-back.

## 6. Remaining commercial policy blocker (unchanged by this task)

The ledger stores the customer-paid shipping charge as `PendingAllocation` and nothing more. Deciding
who owns that money — carrier payable, seller share, marketplace share, discounts, shipping tax,
funder — is still an open commercial policy owned by the Architect. This task deliberately created no
such value and left every such column NULL. Any future allocation policy must not be inferred from this
document; the nullable columns are the extension point.

## 7. Files

Source (see the Result for the exact diff):

* `src/backend/Modules/Order/Tooba.Order.Contracts/Settlement/OrderSettlementShippingGroupContracts.cs`
* `src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Settlement/OrderCheckoutSalesChannelSettlementBridge.cs` (new)
* `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs`
* `src/backend/Modules/Settlement/Tooba.Settlement.Application/Payouts/Ports/SettlementChannelLedgerEligibilityGate.cs` (new)
* `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/DependencyInjection/SettlementMarketplaceChannelLedgerOptions.cs` (new)
* `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/DependencyInjection/SettlementModule.cs`
* `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Gateways/SettlementChannelLedgerEligibilityGate.cs` (new)
* `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence/SettlementLedgerSchemaReadiness.cs` (new)
* `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Handlers/SettlementEventHandlers.cs`
* `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Observability/SettlementInstrumentation.cs`
* `src/backend/Host/Tooba.Host/appsettings.json` (`Tooba:Settlement:MarketplaceChannelLedger:Enabled=false`)
* `src/backend/Modules/Settlement/Tooba.Settlement.Tests/Behavior/SettlementChannelLedgerEligibilityTests.cs` (new)
* `src/backend/Host/Tooba.Host.Tests/SettlementEligibilityTestGate.cs` (new)
* `src/backend/Host/Tooba.Host.Tests/MarketplaceSettlementEventPathTests.cs`, `R2LiveMarketplaceSmokeTests.cs`

Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-143/`.

## 8. Rollback

Reverting the source commit restores the previous `Edition==Marketplace`-only registration and removes
the per-event gate. `Tooba:Settlement:MarketplaceChannelLedger:Enabled` defaults to `false`, so no
deployment acquires the capability by accident; operational rollback is a configuration change only.
The disposable scratch environment is destroyed and needs no rollback.
