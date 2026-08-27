# 21 — Acceptance checklist (TB-P06-T017)

| # | Criterion | Status |
|---|---|---|
| 1 | Story module Domain/Application/Infrastructure + schema `story` | Done |
| 2 | Migration `20260827070104_InitialStory` registered | Done |
| 3 | Lifecycle Draft/Scheduled/Active/Expired/Disabled | Done |
| 4 | Public `GET /v1/storefront/stories?locale=&market=` | Done |
| 5 | Admin CRUD + enable/disable/schedule/reorder/items | Done |
| 6 | Seed StoreAlpha: موبایل (fa), بازی (null+video), English rail (en) | Done |
| 7 | Host MapStoryEndpoints + compose + migrate/seed bootstrap | Done |
| 8 | Fake `STORY_IMAGES` removed from production home UI | Done |
| 9 | Exact Shopeiva rail+modal port (`#E53935`, Swiper, progress/tap RTL) | Done |
| 10 | Live FE binding via `story-api.ts` | Done |
| 11 | Customer AddStory omitted (admin creates) | Done |
| 12 | Composition section `stories` still renders HomeStoriesSection | Done |
| 13 | Locale filter: fa excludes English rail; en includes it | Done |
| 14 | Unsafe CTA rejected | Done |
| 15 | Backend StoryFoundationTests | Done |
| 16 | Frontend story-api + home-structure guard tests | Done |
| 17 | Browser captures 01–04 + proof script | Done / run at evidence time |
| 18 | Readiness may report `STORY_LIVE_WITH_EXACT_SHOPEIVA_UI` only | Done |
| 19 | NOT `PRODUCT_FULLY_READY` | Done |
| 20 | Zero redesign declaration | Done |

Architect ACCEPT is separate from Worker PASS.
