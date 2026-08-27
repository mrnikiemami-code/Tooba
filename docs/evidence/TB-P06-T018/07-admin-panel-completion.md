# 07 — Admin panel completion (TB-P06-T018)

## Selected Admin gap closed

### Hide settings from primary navigation

- `/admin/settings` is **not** a live commercial module.
- Wave 1 hides Settings from Admin primary nav so operators are not led into a stub.
- Route may remain as an honest unavailable / capability page if deep-linked; it must not fake KPI or infra save.

## Unchanged live Admin surfaces (prior tasks)

| Surface | Status |
|---|---|
| Dashboard / products / orders | LIVE |
| Fulfillments / returns | LIVE |
| Settlement / payouts | LIVE |
| Content / stories / page composition | LIVE |
| Sellers / customers / reviews | LIVE |

## Explicit non-claims

- Admin settings module **not implemented**.
- No tenant/infra secret editor.
- No fake disabled action buttons presented as “coming soon” in primary nav.
- No SpiceDB model change for admin settings (module deferred).

## Visual / shell

- Admin shell remains Shopeiva Vendor/Account-derived operational chrome.
- No second design language introduced for the settings hide.
