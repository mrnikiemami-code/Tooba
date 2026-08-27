# 04 — Seed recheck

Task: TB-P06-T026-R1

## Method

Restart Host (Development) so `WalletDevelopmentSeedHost` re-applies. No direct SQL mutation.

## Observations

| Check | Result |
|-------|--------|
| Balance after re-seed restart | **850000** unchanged vs pre-restart (no duplicate ledger credits) |
| Ledger total | **3** before additional R1 redeem; then **4** after spare redeem |
| Spare redeem `TOOBA-DEMO-GIFT-SPARE` | +250000 → balance **1100000**; idempotent replay true |
| Expired `TOOBA-DEMO-GIFT-EXPIRED` | **400** `wallet.redeem.rejected` |
| Consumed primary/spare not reusable | Redeemed status; remaining 0 |
| Repair preview card | Seed ensures `TOOBA-DEMO-GIFT-R1` when spare consumed |
| Production seed path | none (Development Host only) |

Post-repair demo-preview: code `TOOBA-DEMO-GIFT-R1`, balance **1100000**, card `01900000-0000-7000-9000-000000000026`.
