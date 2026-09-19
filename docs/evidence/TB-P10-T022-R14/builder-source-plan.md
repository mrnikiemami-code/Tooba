# Builder Source Integration Plan

## User-facing

نمایش کالا → منبع محتوا → **پیشنهاد شگفت‌انگیز**

## Internal

| Item | Value |
|---|---|
| Persisted `source` | `PromotionCampaign` (add to `ProductSources`; do not reuse unimplemented `Discounted`) |
| Config | `{ source, campaignId?: guid, take, title? }` |
| `campaignId` null | Resolve **current active** AMAZING campaign(s) per recommended query (document single-winner rule) |
| `campaignId` set | Pin that campaign (even if teasing — preview rules TBD; published storefront should still enforce window) |
| Advanced multi-campaign picker | Later; reuse Admin AppDataGrid/selector — **no raw GUID primary UI** |
| Empty | Empty rail + admin hint |
| Preview | Composer resolve against Dev DB membership |
| Published | Same resolver on public Landing/Home |
| Locale | Inherit Page.Locale for badge/title |
| Store scope | Tenant DB |

## UI notes

- Do not expose full catalog dropdown
- Label Persian for users; stable English/internal source key in JSON
- Keep Manual/Category/Brand/Newest unchanged
