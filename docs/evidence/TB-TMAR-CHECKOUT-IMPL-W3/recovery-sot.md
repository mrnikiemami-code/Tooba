# Recovery SoT — TB-TMAR-CHECKOUT-IMPL-W3
- Cart.Contracts conversion seam (ICartConversionPort)
- Order.Application→Cart.Application removed; uses Cart.Contracts
- Process Manager integrates Cart + Inventory contracts; TX preserved
- Residual: Order.Infrastructure→Cart.Application; Order.Infrastructure→Inventory.Application; Order.Application→Promotion.Application
- W4 READY; candidate Order.Infra Inventory.Contracts cleanup
- Orders FE STILL_WAITING_FOR_BACKEND_W4
- Next: TB-TMAR-CHECKOUT-IMPL-W4
- Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL
