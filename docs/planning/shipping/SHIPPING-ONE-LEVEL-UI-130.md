# SHIPPING-ONE-LEVEL-UI-130 — Settlement per-group shipping fee accounting (M4)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-130` (Parent `TB-SHIPPING-ONE-LEVEL-TABS-129`)
Mode: `BOUNDED_SETTLEMENT_SHIPPING_GROUP_LEDGER_M4`

## What was added (owner-correct, additive)

Settlement now owns a durable per-group shipping-fee accounting representation keyed by the stable
`ShippingGroupId` plus the payment / order / seller identities:

- table `settlement.shipping_group_fees` (migration `20261016140000_AddSettlementShippingGroupFees`,
  additive raw SQL, no model-snapshot change — same shape as the TB-129 Fulfillment migration).
- domain aggregate `ShippingGroupFeeAllocation` with `PendingAllocation` state and explicit
  KNOWN / UNSPECIFIED value split.

Only the **authoritative known** content is persisted:

| field | state |
| --- | --- |
| `customer_charge_amount` + `currency` | KNOWN (accepted Order quote, reconciled to the paid Payment group allocation) |
| `accepted_quote_version` | KNOWN (Order-owned accepted-quote marker, never invented) |
| `allocation_policy_version` / `allocation_policy_reference` | UNSPECIFIED (`NULL`) |
| `carrier_payable_amount` | UNSPECIFIED (`NULL`) — no carrier tariff authorized |
| `seller_share_amount` / `marketplace_share_amount` | UNSPECIFIED (`NULL`) — signed shares, no policy authorized, no marketplace residual assumed |
| `shipping_discount_amount` | UNSPECIFIED (`NULL`) |
| `shipping_tax_amount` | UNSPECIFIED (`NULL`) |
| `funder_adjustment_amount` / `funder_reference` | UNSPECIFIED (`NULL`) |

Missing values are **never** zero-filled: the factory accepts no share parameter at all, so the only
reachable state is `PendingAllocation` with every commercial field `NULL`.

## Fail-closed guarantees

- The row is never a `SellerOrder` merchandise ledger entry: `EnsureNotSellerRevenue()` always raises
  `settlement.shipping_group.not_seller_revenue`, and the recording path never writes
  `SettlementAccount` / `SettlementEntry` / `PayoutRequest`.
- Unknown commercial shares are blocked from settlement/payout:
  `EnsurePayoutAllocationAvailable()` raises `settlement.shipping_group.allocation_policy_missing`
  while the row is `PendingAllocation`.
- Accepted-charge integrity: the accepted Order group charge must equal the Payment
  `StoreShippingGroup` allocation for the same group (`ShippingGroupId`, amount, currency); otherwise
  `settlement.shipping_group.charge_mismatch` and nothing is recorded. Recording happens *before*
  seller accrual in the `payment.succeeded` handler, so a mismatch aborts before any seller credit.
- A group with no paid allocation is never recorded; a zero-charge (free) group carries no customer
  shipping fee and produces no settlement row.
- The legacy ungrouped `StoreShipping` sentinel route is untouched: an ungrouped order yields no group
  rows and the accrual path behaves exactly as before.

## Reconciliation invariant

For every payment: for each accepted stable group with a positive customer charge, exactly one
immutable `Charge` row exists with `customer_charge_amount` equal to the accepted quote charge, which
equals the Payment per-group `StoreShippingGroup` allocation for that `ShippingGroupId`; and

```
SUM(charge rows for payment) == SUM(payment StoreShippingGroup allocations)
```

Group fee rows contribute **0** to `SettlementAccount` balances and payout availability, because no
authorized share exists. Correctness of the group fee rows is independent of (and cannot leak into)
seller revenue.

## Versioned policy seam (documented, not invented)

A deterministic, versioned `ShippingAllocationPolicy` seam must eventually supply, per group:
carrier-payable cost (tariff/settlement), signed seller and marketplace shares, discount/subsidy
funding source, shipping tax, and any funder/adjustment. Its version/reference will be persisted into
`allocation_policy_version` / `allocation_policy_reference` and the row will move out of
`PendingAllocation` only when the full balanced allocation is available. **No** percentage, tariff,
subsidy, tax rate or payout entitlement is created by this task.

## History immutability

Accepted charge rows are immutable. Corrections are expressed as `Reversal` rows referencing
`reversal_of_shipping_group_fee_id` (unique index keeps one charge row per `(payment, group)`);
reversals carry no financial amounts and are themselves `PendingAllocation`.

## Boundaries respected

- No CSS, theme, component, visual or checkout refactor; storefront UI is identical.
- No Host business authority, no new public endpoint, no manual SQL, no production activation.
- Shared `:5088` / `:3000` and `tooba_alpha` were read-only; no process start/stop, no shared DB writes.

## Verification

- `dotnet test src/backend/Modules/Settlement/Tooba.Settlement.Tests` → 39/39 pass (9 new per-group tests).
- `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~SettlementModuleAmsc001`
  → 37/37 pass (API-code split, migration allowlist and cert guards updated for the additive migration).
- `dotnet test ... --filter FullyQualifiedName~MarketplaceSettlementEventPathTests` → 2/2 pass against a
  real PostgreSQL container; this applies the new migration and exercises the new handler path, so the
  migration is PROVEN at runtime.
- Pre-existing unrelated failure: `SettlementFoundationTests.Settlement_lifecycle_applies_commission_refund_and_payout_safety`
  expects `InvalidOperationException` while `RequestPayoutAsync` / `AdjustFromRefundAsync` throw the typed
  `ContractOperationException` (which is not an `InvalidOperationException`). Those assertions and code
  paths exist unchanged at the task base SHA and are not touched by TB-130.
