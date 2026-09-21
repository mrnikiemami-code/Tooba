# Cross-module boundary audit

| Consumer | Before | After |
|---|---|---|
| Fulfillment.Infra | Order/Inventory/Payment.Application | Order/Inventory/Payment.Contracts |
| Returns.Infra | Order/Fulfillment/Payment/Inventory.Application | Order/Fulfillment/Payment/Inventory/Wallet.Contracts |
| Settlement.Infra | Returns.Application (+ Order.Application for returns reader) | Returns.Contracts.Settlement + Order.Contracts.Returns |
| Inventory.Infra | owned Application Returns gateway | Implements Contracts.Fulfillment lifecycle + Contracts.Returns gateway |
| Payment.Infra | Application refund gateway types | Contracts.Returns + PaymentReturnBridge |

No foreign DbContext. No TypeForwardedTo. Accidental behavior change target: 0.
