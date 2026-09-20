# Order→Inventory dependency
- Removed Order.Application → Inventory.Application
- Now Order.Application → Inventory.Contracts
- Residual: Order.Infrastructure → Inventory.Application (Cancel/Restore/PaymentBridge)
