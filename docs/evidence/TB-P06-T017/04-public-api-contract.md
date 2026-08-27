# 04 — Public API contract (TB-P06-T017)

## Endpoint

```http
GET /v1/storefront/stories?locale={locale}&market={market}
```

Registered in `Tooba.Host/Story/StoryEndpoints.cs` → `MapStoryEndpoints`.

## Query

| Param | Required | Behavior |
|---|---|---|
| `locale` | No | Filters by story locale (null story locale always included) |
| `market` | No | Filters by story market |

Tenant resolved from current Host tenant context (`StoryPanelComposer.RequireTenantId`).

## Response

JSON array of `PublicStoryCard`:

| Field | Type |
|---|---|
| `storyId` | guid |
| `title` | string |
| `coverMediaUrl` | string? |
| `isVideo` | bool (cover/items include video) |
| `displayOrder` | int |
| `ctaType` / `ctaTarget` | string / string? |
| `items[]` | `storyItemId`, `mediaType`, `mediaUrl`, `caption`, `durationMs`, `ctaType`, `ctaTarget` |

Only publicly visible Active stories with schedule window satisfied are returned, ordered by `displayOrder`.

## Runtime proof (evidence-time)

| Call | Result |
|---|---|
| `GET http://127.0.0.1:5088/v1/storefront/stories?locale=fa` | 200 — 2 cards: **موبایل**, **بازی** (English rail excluded) |
| `GET ...?locale=en` | 200 — includes **English rail** (+ locale-null **بازی**) |
| FE proxy `http://127.0.0.1:3000/v1/storefront/stories?locale=fa` | 200 — same live payload |

Auth: none (public storefront). Missing tenant → 400 `story.tenant.missing`.
