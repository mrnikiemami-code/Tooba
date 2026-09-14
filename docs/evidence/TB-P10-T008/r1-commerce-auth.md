# TB-P10-T008-R1 — Commerce/auth regression

No commerce or auth code changed except pending-payment hold chip `bg-[#EFF6FF]` → `bg-surface` (color only).

`npm run test:storefront` 67/67 covers identity, merge, Cart Active, COMMIT, 409-on-Shipping. `storefront-identity-api` and shipping/checkout suites green.
