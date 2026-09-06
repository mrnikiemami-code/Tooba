# R2 — Marketplace runtime

## Host edition

`Tooba__Edition=Marketplace`, `DeploymentId=dev-marketplace-r2` on `:5088`:

```json
{"edition":"Marketplace","deploymentId":"dev-marketplace-r2","connectionReference":"marketplace"}
```

DB: `tooba_marketplace`. Messaging: `tooba_messaging` / `transport.*` present.

## Scenario (Marketplace handlers only — no Accrue/Adjust shortcut)

Opt-in live smoke `TOOBA_R2_LIVE=1` → `R2LiveMarketplaceSmokeTests` against `tooba_marketplace`.

| Field | Id / amount |
|-------|-------------|
| sellerOrderId | `a366d5c2-95a1-4080-ae52-63ebe2f6e16f` |
| checkoutId | `3996ba84-269b-438d-949e-786b353765d6` |
| paymentId | `408be279-e23d-480e-b4f1-be34a8953d87` |
| returnRequestId | `cc7c5262-f9e1-4215-9456-006b08019635` |
| credit | `56511313-5787-49f1-b87d-20d75310d931` gross `218000` net `196200` |
| debit | `a7d6930b-248d-4774-89a6-ba8426fe17a7` gross `218000` net `196200` |
| balanceAfter debit | prior − `196200` |

Flow: PaymentSucceeded consumer → credit; Returns refund Completed → RefundSucceeded consumer → debit; consumer redelivery → still one debit; credit unchanged.

Also: `MarketplaceSettlementEventPathTests` (Testcontainers, Edition=Marketplace) green.
