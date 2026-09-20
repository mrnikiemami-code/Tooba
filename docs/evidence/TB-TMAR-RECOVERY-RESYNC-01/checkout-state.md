# Checkout State

- **Last completed Checkout task:** `TB-TMAR-CHECKOUT-IMPL-W5` (`d73d2aae` / Bridge Completed)
- **Paused/active:** **PAUSED_AT_SAFE_W5_CHECKPOINT** (W6 deferred; not active)
- **Next Checkout task canonical?** `TB-TMAR-CHECKOUT-IMPL-W6` is READY in W5 SoT but **intentionally not issued** while Offer track / user review gate applies
- **Shared TransactionScope:** preserved (W1–W5); ARCH-TX-001 / ARCH-CHECKOUT-001..005 active; no Saga rewrite
- **Process Manager:** Order-owned in-process foundation from W1/W2; still in-process
- **Inventory Contracts:** reservation + lifecycle ports in use (W2/W4)
- **Cart Contracts:** ICartConversionPort (W3)
- **Promotion Contracts:** ICheckoutPromotionPort (W5)
- **Order.Application foreign Application edges:** none remaining to Cart/Inventory/Promotion/Offer/Pricing/Tax Application — Contracts only (csproj verified)
- **Order.Infrastructure foreign Application edges still present:** Cart.Application, Catalog.Application, Payment.Application (known residual debt; Cart.Contracts also referenced). Inventory/Promotion via Contracts.
