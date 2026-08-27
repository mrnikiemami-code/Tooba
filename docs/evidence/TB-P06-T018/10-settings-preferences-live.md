# 10 — Settings / preferences live (TB-P06-T018)

## Honesty rule

Any **visible** Settings entry in primary nav must lead to a live, honest page — never a fake save.

## Customer settings (`/customer-panel/settings`)

| Preference | Wave 1 status | Mechanism |
|---|---|---|
| Profile / account identity | LIVE bridge | Link + Host `/v1/customer/profile` via profile page |
| Locale preference | LIVE | Locale cookie preference (storefront locale foundation reused) |
| Security (password / sessions / 2FA) | UNAVAILABLE | Honest copy; no fake mutation |
| Notification preferences | UNAVAILABLE | Honest copy; no fake mutation |

## Seller settings (`/vendor-panel/settings`)

| Preference | Wave 1 status | Mechanism |
|---|---|---|
| Operational seller context | LIVE | Seller dashboard API read model |
| Business profile edit | DEFERRED | No fake save UI |
| Store secret / infra config | FORBIDDEN | Never exposed |

## Admin settings (`/admin/settings`)

| Preference | Wave 1 status | Mechanism |
|---|---|---|
| Admin settings module | DEFERRED | Hidden from primary nav; route remains honest unavailable |

## Secrets / infra

- No raw connection strings, SpiceDB tokens, or Host secrets in settings UIs.
- No tenant infra editor introduced.

## Non-claims

- Notification preference persistence requires Notifications foundation (see `08`).
- Seller business profile mutation requires a dedicated seller profile API + ownership rules (deferred).
