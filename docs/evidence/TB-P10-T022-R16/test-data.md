# TB-P10-T022-R16 — Test Data / Seed

## Gate

`MerchandisingCampaignDevelopmentSeed.EnsureAsync` — **Development only** (`IHostEnvironment.IsDevelopment()`). Never runs in Production.

Called from `ProductWorkspaceDevelopmentBootstrap` after AMAZING type seed (both first-seed and refresh paths).

## Scenarios (relative to `DateTimeOffset.UtcNow`)

| Id | Marker | Lifecycle | Window | Priority |
|----|--------|-----------|--------|----------|
| `…0001` | `[DEV-SEED] Amazing Active Primary` | Published | now-2h … now+7d | 100 |
| `…0002` | `[DEV-SEED] Amazing Active Loser` | Published | now-1h … now+3d | 10 |
| `…0003` | `[DEV-SEED] Amazing Future Teasing` | Published | now+1d … now+2d | 80 |
| `…0004` | `[DEV-SEED] Amazing Expired` | Published | now-3d … now-1d | 90 |
| `…0005` | `[DEV-SEED] Amazing Draft` | Draft | overlapping | 50 |

Members: up to 8 real Active offers + one OOS offer (`SellerSku=DEV-SEED-OOS`, Available drained to 0) on Primary.

## Idempotency

Well-known campaign Guids + `UpsertSeedCampaignAsync` + `SyncSeedMembersAsync` + translation upsert. Re-run refreshes windows without duplicates.

If fewer than 5 Active offers exist in DB, seed uses what is available (documented here).
