# TB-281 RESULT (worker)

Status: **PARTIAL** within issued first Story marketplace backend delivery.
Decision: `STORY_SHOP_OWNERSHIP_MEDIA_SELLER_SCOPE_PUBLICATION_POLICY_VERSION_FENCED_LIFECYCLE_AND_TRANSPORT_PLUMBING_LANDED_WITH_FOCUSED_PROOF_AND_ADDITIVE_SCHEMA_APPLIED_BUT_FULL_G_SCENARIO_MATRIX_AND_ADMIN_SHOP_NAME_GRID_FILTER_REMAIN_OPEN`
NOT Architect ACCEPT. NOT user functional/visual acceptance. Delivery two NOT issued.

## Claim

- Expected-Base-SHA: `6c0914c37eb9de1e915c874718d813270cc8d9f0` (verified ancestor)
- Claim HEAD (docs-only issuance): `c15a3a69c15675ae68acb7e001e01bf373f1d0b0`
- Bridge row: `e63e0bdb-96a2-4bc7-ab1c-b32f8524eaef`
- Worker: `tooba-worker-01` / channel `tooba-main`

## Delivered

### A Admin-for-shop
- `CreateShopOwnedDraft` + `AdminCreateForShopAsync` with Party status projection gate (`story.shop.invalid`)
- Distinct `CreatedByActorUserId` vs `SellerPartyId`; seller list/get scoped by shop
- Composer routes admin create with `SellerPartyId` to for-shop path

### B Seller media ownership
- Additive Media binding + occurrence tables (`Tb281SellerAssetOwnerScope`)
- `IStoryMediaAssetPort` authorize/upload/list; seller routes `/v1/seller/media`
- Story seller add/update item binds via Media port; raw URL-only seller media refused (`story.media.forbidden`)

### C Readiness + effective publication
- `StoryPublicationPolicy` owns playable content + effective public windows (start inclusive, end exclusive)
- `ApproveAndPublish` / `ApproveAndSchedule` atomic commands; submit/activate require playable content

### D Version / replay / lifecycle
- `EnsureExpectedVersion` required before effectful approve/publish/return/stop
- Occurrence replay for admin-for-shop create; seller `return-to-draft` / `stop`
- Additive Story `CreatedBy` + `command_occurrences` migration applied

### E Contracts + FE transport (bounded)
- Snapshot fields: shop display, contentReady, effectivelyPublic, actionCodes, createdBy
- FE `story-api` maps new fields; create-for-shop + approve-and-publish/schedule + seller return/stop send expectedVersion/occurrence/sellerPartyId
- NO shared editor/grid restyle (delivery two)

### F Schema
- Fresh `pg_dump` `D:\temp\tb281-backup\tooba_alpha-pre-tb281.dump` bytes `3061329` SHA256 `7551A1A12E5758C21ACBBACC1DB52609ACA75D3C403331B6F36A9B78D8B7CD42`; `pg_restore --list` TOC 1475
- Plan pending only Media `20261118233932_Tb281SellerAssetOwnerScope` + Story `20261118233937_Tb281CreatedByAndOccurrences`
- Applied once via `Tooba.MigrationRunner apply --tenant store-alpha`; post-apply pending `[]` for Media/Story
- ZERO live Story/Media business writes

### G Focused proof
- `StoryMarketplace281DomainTests` 4/4
- Story/Media AMSC pin suite filter 52/52
- FE `story-api.test.ts` 3/3; `tsc --noEmit` 0
- Host publish 0 errors; Host `5088` refreshed dll SHA256 `5B1CDCF438101EE8A99F734C13E5C9E535C14C90955355ADD7988EEA62AA3E17`; `/health` 200; `/health/ready` 200 SingleStore + messaging Healthy

## Remaining (why PARTIAL)

1. Full issued G1–G7 cohesive directory/media harness not completed (no controlled object-store seller upload suite; cross-tenant/B isolation directory facts incomplete).
2. Admin grid shop **human-name** search/filter via Party not implemented (SellerPartyId projection exists; name filter outstanding).
3. CTA foreign-shop product refuse not proven end-to-end.
4. Public shop attribution enrichment beyond optional display name incomplete.
5. Served walkthrough limited to health/readiness (no live Story mutation authorized).

## Stop

`STOP_FOR_ARCHITECT`; `automaticNextImplementationTask: NONE`.
