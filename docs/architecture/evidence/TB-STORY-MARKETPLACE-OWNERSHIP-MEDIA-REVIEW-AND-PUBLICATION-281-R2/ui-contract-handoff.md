# UI contract handoff (TB-281-R2 backend) — PLANNED UI NOT ISSUED

## Seller

- `GET /v1/seller/stories` — list (enriched ShopDisplayName when Party available)
- `POST /v1/seller/stories/query` — body GridQueryRequest `{page,pageSize,search,sort,filters,advancedFilter}`; response `{items,page,pageSize,totalCount}`
- `POST /v1/seller/stories` — create; optional `occurrenceKey`; cover requires owned Ready media asset id (no raw URL)
- `PUT /v1/seller/stories/{id}` — `expectedVersion` required for effectful update; cover/CTA revalidated
- `POST …/stop` body `{expectedVersion, occurrenceKey?}` — replay before stale when occurrence matches
- `POST …/return-to-draft` same shape; after stop (Disabled+Approved) actionCodes include `return_to_draft`
- `POST …/items` optional `occurrenceKey` + `expectedVersion`
- Product CTA: `ctaType=product`, `ctaTarget=<product Guid owned by shop>` else `story.cta.rejected`

## Media

- `POST /v1/seller/media` multipart `file` + `occurrenceKey` (required) → StoryMediaAssetAuthorization
- `GET /v1/seller/media`, `GET /v1/seller/media/{id}`
- `POST /v1/admin/media/upload-for-shop` multipart `file` + `sellerPartyId` + `occurrenceKey` (Active shop only)

## Admin grid

- Filter fields `shopDisplayName` / `shopName` (contains/eq/startswith/endswith) over Party projections for shops present on Story rows (complete for that set; not truncated global Party search).

## Public

- Cards may include `shopDisplayName`; `shopPublicId` remains null unless a canonical public identity port exists (do not fabricate from Party Guid).

## Versions / errors (stable)

`story.version.required`, `story.version.stale`, `story.occurrence.conflict`, `story.media.forbidden`, `story.media.unavailable`, `story.cta.rejected`, `story.shop.invalid`, `story.shop.authority.unavailable`
