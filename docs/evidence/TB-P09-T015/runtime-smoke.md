# Runtime smoke — TB-P09-T015

Host `127.0.0.1:5088` `Host: alpha.localhost`. Script: `_runtime.mjs`.

- ru-RU UoM: килограмм / кг
- Product kg places=2 step=null
- Arman offer Min=0.50 Max=20
- Kg offer OnHand=10.75; cart 1.25 reserves 1.25; Available 9.50
- Checkout `01a084a4-4138-7000-a7d1-676e53356f73` OrderLine 1.25 kg/2/null
- Header weighted Floor: `Floor|0|1247.5|249|998.5|89|0|89|1087.5|1|1.25` (qty 1.25 × 998)
- Canonical Floor 998×20%: checkout `01a08450-f2b0-7000-816d-552bb5fd54cd` `Floor|0|998|199|799|71|0|71|870|1|1`
- Tax+Duty = Tax + Duty (89+0=89; 71+0=71)
- StartProcessing + pack 0.50 remain 0.75 + ship 0.50 + dispatch/deliver + return 0.25
- List `lineCount=1` from Header (not TotalQuantity)
- After GlobalRoundingMode → Ceiling, historical header remains Floor
- Invoice HTML: تعداد اقلام / عوارض / جمع مالیات و عوارض
- Step null 1 / 1.2 / 1.25; Step 0.25 on 1.37 → 1.25 / 1.50 / 1.25 (tests)

Rounding restored to Nearest. Kg price restored. Promo coupons expired.
