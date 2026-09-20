# Layering
- Order.Infrastructure → Inventory.Contracts only for lifecycle seam
- Adapter Inventory-owned
- No new App→App
- No Infra→foreign Domain (StockReservationStatus removed from OrderPaymentBridge)
- No service locator / reflection workaround
