# 05 — Commission policy snapshot (TB-P06-T012)

## Default marketplace policy

Defined in `CommissionPolicy.CreateDefaultMarketplace`:

| Field | Value |
|---|---|
| PolicyId | `00000000-0000-0000-0000-000000000010` |
| Name | `marketplace-default-10pct` |
| Rate | `0.10` (10%) |
| IsDefault | `true` |

## Snapshot immutability

`CommissionPolicySnapshot.FromPolicy` copies `PolicyId`, `PolicyName`, `Rate` onto each posted entry.

Changing the live default policy does **not** rewrite historical entries — commission at time of accrual is preserved.

## Persistence

Tables in schema `settlement`:

- `commission_policies` — active policy catalog
- Entry column group `CommissionPolicySnapshot` (owned type on `settlement_entries`)

## Test proof

`SettlementFoundationTests` asserts on first credit entry after payment accrual:

- `entry.CommissionPolicySnapshot.Rate == 0.10m`
- `entry.CommissionAmount == round(gross * 0.10, 4)`
- `entry.NetAmount == gross - commissionAmount`

Example gross 109,000 IRR → commission 10,900 → net 98,100.
