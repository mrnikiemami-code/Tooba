# 17 — Final validation proof (TB-P06-T017)

## Frontend

```text
npm run test:stories  → pass 2
npm run test:home     → pass 6 (includes live stories guard)
npm run build         → success (Next.js production build)
```

## Backend (focused)

```text
dotnet test Host.Tests --filter StoryFoundationTests
→ Passed: 2, Failed: 0
```

Covers: module boundary; public seed visibility; draft/scheduled/expired/disabled hidden; unsafe CTA rejected; admin auth; reorder; locale filter.

## Browser / HTTP acceptance

```text
node scripts/prove-t06-t017-stories.mjs
→ pass: true
→ captures 01–04 under docs/evidence/TB-P06-T017/captures/
→ _acceptance-proof.json
```

## Readiness claim

`STORY_LIVE_WITH_EXACT_SHOPEIVA_UI` — not `PRODUCT_FULLY_READY`.
