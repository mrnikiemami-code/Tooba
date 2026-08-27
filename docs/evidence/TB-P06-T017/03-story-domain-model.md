# 03 — Story domain model (TB-P06-T017)

Source: `src/backend/Modules/Story/Tooba.Story.Domain/StoryEntities.cs`

## Lifecycle statuses (`StoryStatus`)

| Value | Name | Public visibility |
|---|---|---|
| 0 | `Draft` | Hidden |
| 1 | `Scheduled` | Hidden until window |
| 2 | `Active` | Visible when `StartAt`/`EndAt` allow |
| 3 | `Expired` | Hidden |
| 4 | `Disabled` | Hidden (soft disable) |

Public rule: `Status == Active` AND (`StartAt` null or ≤ now) AND (`EndAt` null or > now).

## `Story` fields

| Field | Notes |
|---|---|
| `StoryId` | Uuid v7 |
| `TenantId` | Tenant-scoped |
| `Locale` | Optional; `null` = all locales |
| `Market` | Optional; independent of locale |
| `Title` | ≤ 120 |
| `CoverMediaAssetId` / `CoverMediaUrl` | Cover |
| `DisplayOrder` | Rail order |
| `StartAt` / `EndAt` | Schedule window |
| `Status` | Lifecycle |
| `CtaType` / `CtaTarget` | Story-level CTA |
| `VersionToken` | Concurrency touch on mutate |
| `Items` | Ordered `StoryItem` collection |

## `StoryItem` fields

| Field | Notes |
|---|---|
| `StoryItemId` | Uuid v7 |
| `StoryId` | Parent |
| `DisplayOrder` | Slide order |
| `MediaType` | `image` \| `video` |
| `MediaAssetId` / `MediaUrl` | Media |
| `Caption` | ≤ 200 |
| `DurationMs` | Optional duration (≥ 0) |
| `CtaType` / `CtaTarget` | Item-level CTA |

## CTA rules (`StoryRules.ValidateCta`)

Allowed types: `none`, `product`, `category`, `article`, `internal`, `external`.

- `none` → target forced `null`.
- Any other type → target required.
- Forbidden target prefixes (case-insensitive): `javascript:`, `data:`, `vbscript:`.
- Rejected with domain error → Host maps to `story.cta.rejected` (400).

## Locale / market match

- Empty story locale → matches any request locale.
- Empty request locale → no locale filter.
- Otherwise exact or language-prefix match (`fa` ↔ `fa-IR`).
- Market analogous; empty either side passes.
