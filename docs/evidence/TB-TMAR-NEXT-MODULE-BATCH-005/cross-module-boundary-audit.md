# Cross-module boundary audit — BATCH-005

## Cart
| Edge | Before | After |
|------|--------|-------|
| Cart.Application → Catalog | Application | Contracts (`ICatalogCartQuantityPolicyGateway`) |
| Cart.Application → Inventory | Application | Contracts (`ICartInventoryHoldPort`) |
| Cart.Application → Offer/Pricing | Contracts | Contracts (unchanged) |
| Cart.Infrastructure foreign Application | none required | Contracts only |

## Settlement
| Edge | Before | After |
|------|--------|-------|
| Settlement.Infrastructure → Payment | Application (+ Domain usings) | Payment.Contracts |
| Settlement.Infrastructure → Order | Application | Order.Contracts |
| Settlement.Infrastructure → Returns | Application | Returns.Contracts |

## Host (residual)
| Edge | Status |
|------|--------|
| Host SettlementPanelComposer → SettlementDbContext | PRESENT (blocking) |
| Host AdminPayoutGridQueryEngine → SettlementDbContext + PartyDbContext | PRESENT (blocking) |

## Verdict
Module ProjectReference boundaries for Cart + Settlement Application/Infrastructure: CONTRACTS_ONLY.
Host Settlement query authority: NOT CLEAR.
