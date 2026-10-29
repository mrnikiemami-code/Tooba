# RECEIPTS — TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4

## Identity
- TaskId: TB-STOREFRONT-NAVIGATION-LATENCY-AND-PENDING-FEEDBACK-245-R4
- Bridge row: 6bc0fddc-4133-439a-8a90-d35ca9877242
- Base / ISSUE: c659914b346350e52b71d7de4e46c160bda4b10f
- Expected-Base-SHA (task header): 76d9c4a7743e8ca7f95c0b8d39bec2404be9b1da (parent Evidence; issue commit fast-forwarded to c659914b3)
- FE http://127.0.0.1:3000 → Host 5088 SingleStore/store-alpha/tooba_alpha
- Cart 01a17b2d-e219-7000-b3b2-5bcdbe6810c5 Active v48 qty18 preserved

## Focused checks (32/32)
- storefront-cart-navigation.guard.test.ts
- storefront-cart-ui.guard.test.ts
- storefront-navigation-pending.guard.test.ts
- lib/storefront-navigation/navigation-pending.test.ts
- storefront-shipping-destination-choice.guard.test.ts
- npm run typecheck clean
- npm run build exit 0

## Live warm cart CTA
- pendingMs ≈ 9; loadingMs ≈ 337; usableMs ≈ 393
- homeBeforeUsable = 0; homeAfterUsable = 1; cart-recommendations present
- path /fa → /fa/cart; beforeunload 0
- ownership after first open: mount cart/pending/login = 1; recommendations = 1; mini-cart-open = 1
- three cycles: per-cycle deltas stable at 1/1/1

## Host cart re-read
- OTP owner 01a0996c-b8d9-7000-9854-80b3f59e8d7c; Active v48; qty 18; lines 4
