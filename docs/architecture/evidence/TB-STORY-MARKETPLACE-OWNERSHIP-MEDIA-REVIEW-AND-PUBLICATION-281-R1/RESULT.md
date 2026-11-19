# TB-281-R1 RESULT (worker)

Status: **PARTIAL** within issued R1 closure of unfinished A–G plus B1–B7.
Decision: `PERSISTED_PUBLIC_READER_ROLE_SCOPED_ACTIONS_FAIL_CLOSED_MEDIA_PARTY_CLASSIC_VERSION_FENCE_AND_ATOMIC_SELLER_UPLOAD_INTENT_LANDED_WITH_FOCUSED_PROOF_AND_ADDITIVE_MEDIA_INTENT_HASH_MIGRATION_BUT_SELLER_QUERY_SHOP_NAME_FILTER_CTA_FULL_G_MATRIX_AND_MEDIA_CQRS_CLEANUP_REMAIN_OPEN`
NOT Architect ACCEPT. NOT user acceptance. UI delivery two NOT ISSUED.

## Claim

- Expected-Base-SHA: `860c6a2c72842c8971d8b9f9885e26cdb0f3f657` (verified == HEAD at claim; ancestor OK)
- Bridge row: `99c98755-4a95-443e-aea8-2b09e02136e3`
- Worker: `tooba-worker-01` / channel `tooba-main`

## Delivered

### B1 Persisted public reader
- `GetPublicStoriesAsync` loads Active/Scheduled candidates, batch-attaches items, THEN applies `IsPubliclyVisible`/locale/market.
- Focused fresh-context Directory proof: active + due-scheduled positive; before-start and after-end negatives (`StoryMarketplace281R1DirectoryTests`).

### B2 Fail-closed shop/media (partial)
- Null Party → `story.shop.authority_unavailable`; null Media → `story.media.unavailable` before bind.
- Seller cover/item bind refuses raw URL; MIME declared-vs-asset mismatch refused.
- Admin-for-shop create still Party-gated; full admin-for-shop Media upload CQRS seam and cover bind on create remain incomplete.

### B3 Atomic upload / occurrence (partial)
- Seller upload requires nonblank occurrence; digests bytes+normalized type/name into `IntentHash`; same-key changed intent conflicts; binding failure compensates task-owned object+row.
- Additive Media migration `20261119000844_Tb281R1OwnerOccurrenceIntentHash` (composite PK tenant+shop+key + intent_hash) applied after verified backup.
- Controlled object-store recording suite (R3) not completed.

### B4 Version / occurrence (partial)
- Classic admin enable/disable/approve/reject/schedule/update and seller update now call `EnsureExpectedVersion`.
- FE transport sends reviewed `versionToken` on those routes.
- Canonical length-prefixed `HashIntent` for admin-for-shop create.
- Occurrence still not on every seller create/add/submit/lifecycle intent; exact replay-before-version matrix incomplete.

### B5 Role-scoped projection
- `MapAdmin(story, isAdmin)` + grid uses Directory mapper; seller actions exclude admin approve codes; stop/return_to_draft present (`StoryMarketplace281R1DirectoryTests`).

### B6 / B7
- Seller server query, admin shop human-name filter, CTA foreign-product refuse, public shop attribution, Media thin CQRS + Story-tenant constant removal: **OPEN**.

### F Schema
- Parent Media/Story TB-281 migrations untouched.
- New Media-only additive migration applied once via `Tooba.MigrationRunner apply --tenant store-alpha`.
- Pre-apply dump `D:\temp\tb281r1-backup\tooba_alpha-pre-tb281r1.dump` bytes `3070590` SHA256 `4D23B35B53FCEB41B35D42B4FA6B00FE0280B47FC41EAF225957C6D874BCBE63`; `pg_restore --list` TOC 1487 exit 0.
- Parent TB-281 dump verified NOW bytes `3061329` SHA256 `7551A1A12E5758C21ACBBACC1DB52609ACA75D3C403331B6F36A9B78D8B7CD42` (historical archive, not retroactive pre-apply for R1).
- Post-apply Media pending `[]`. ZERO live Story/Media business writes.

### G Proof (focused, incomplete vs issued G1–G7)
- `StoryMarketplace281*` 7/7 (4 domain + 3 R1 directory)
- Story/Media AMSC focused filter 52/52
- FE `npm run test:stories` 6/6; `tsc --noEmit` 0
- Host publish 0 errors; Host `5088` dll SHA256 `96F28D635AD937484887C60B01FF550369A7F23955B8A0B30A23168EABE61FD5`; `/health` 200; `/health/ready` 200 SingleStore + messaging Healthy; `/__platform-commerce` → `store-alpha`
- PG rollback-only suite and controlled Media upload R3/R4/R5 matrix: **NOT DONE**

## Remaining (why PARTIAL)

1. Seller query/page/count + admin shop-name Party filter + CTA owning-product refuse + public shop identity enrichment.
2. Full occurrence/version on every effectful seller/admin path with delimiter-proof conflict + replay-before-stale matrix.
3. Media Endpoints CQRS/tenant-constant cleanup; admin-for-shop upload route.
4. Cohesive G1–G7 including real MediaDirectory recording store + PostgreSQL rollback-only facts.
5. UI shared grid/editor delivery still PLANNED NOT ISSUED.

## Stop

`STOP_FOR_ARCHITECT`; `automaticNextImplementationTask: NONE`.
