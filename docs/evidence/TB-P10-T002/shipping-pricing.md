# Shipping pricing

- Price from backend Rates (`BasePrice` / `FreeAboveSubtotal`); client cannot spoof.
- Free only when config BasePrice=0 or free-above threshold met (`in_person` = 0).
- Runtime A: `post:express` = 200000 IRR; F payable includes shippingAmount.
