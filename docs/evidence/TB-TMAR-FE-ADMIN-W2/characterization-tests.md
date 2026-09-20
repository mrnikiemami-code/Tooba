# Characterization — TB-TMAR-FE-ADMIN-W2

`features/admin-reviews/admin-reviews.characterization.test.ts` — 6/6 PASS

Covers:

- public boundary exports (screen + API)
- thin route composition via `features/admin-reviews`
- Host payload mapping without ActorUserId leak
- screen markers + moderate/query wiring + use client
- admin-api cleared of reviews exports
- denied state for list + moderate

Command: `npm run test:admin-reviews`
