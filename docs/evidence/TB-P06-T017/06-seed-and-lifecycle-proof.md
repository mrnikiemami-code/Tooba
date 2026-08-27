# 06 — Seed and lifecycle proof (TB-P06-T017)

Source: `StoryDevelopmentSeed` (idempotent; skips if tenant already has rows).

## StoreAlpha seed (Active)

| Title | Locale | Media | CTA | Order |
|---|---|---|---|---|
| موبایل | `fa` | 2× image | `internal` → `/products` | 0 |
| بازی | `null` (all) | video + image | `category` → `/offers` | 1 |
| English rail | `en` | image | `internal` → `/products` | 2 |

Bootstrap: `MarketplaceDevelopmentBootstrap` + `ProductWorkspaceDevelopmentBootstrap` migrate `StoryDbContext` then `StoryDevelopmentSeed.ApplyAsync`.

## Lifecycle visibility (tests + domain)

| Status | Public `locale=fa` |
|---|---|
| Seeded Active (موبایل, بازی) | Shown |
| English rail | Hidden for `fa`; shown for `en` |
| Draft | Hidden |
| Scheduled (future StartAt) | Hidden |
| Expired (past EndAt) | Hidden |
| Disabled | Hidden |

## Evidence-time Host probe

- `locale=fa` → ≥2 stories including موبایل + بازی; excludes English rail; includes video (`isVideo` on بازی).
- `locale=en` → includes English rail.
