# UI contract handoff (R1) — transport only

Delivery two (Orders-style shared grid/editor) remains **PLANNED NOT ISSUED**.

## Versioned admin bodies

| Route | Body fields |
| --- | --- |
| POST `/v1/admin/stories/{id}/enable` | `{ expectedVersion }` |
| POST `/v1/admin/stories/{id}/disable` | `{ expectedVersion }` |
| POST `/v1/admin/stories/{id}/approve` | `{ expectedVersion }` |
| POST `/v1/admin/stories/{id}/reject` | `{ reason, expectedVersion }` |
| POST `/v1/admin/stories/{id}/schedule` | `{ startAt, endAt, expectedVersion }` |
| PUT `/v1/admin/stories/{id}` | existing fields + `expectedVersion` |
| POST approve-and-publish / approve-and-schedule | unchanged (already versioned) |
| Seller return-to-draft / stop | unchanged (already versioned) |

## Typed blockers (stable codes)

- `story.version.required` / `story.version.stale`
- `story.media.unavailable` / `story.media.forbidden`
- `story.shop.invalid` / `story.shop.authority_unavailable`
- `story.occurrence.conflict`

## Snapshot fields (retained from 281)

`shopDisplayName`, `contentReady`, `effectivelyPublic`, `actionCodes`, `createdByActorUserId`, `versionToken`

## Not ready for UI delivery two

Seller query endpoint, admin shop-name grid filter, CTA product validation, public shop identity/link, full occurrence keys on every mutation.
