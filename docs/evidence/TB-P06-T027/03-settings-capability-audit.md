# 03 — Settings capability audit

| Capability | Class | Notes |
|---|---|---|
| Customer profile | LIVE | CustomerProfile GET/PUT /v1/customer/profile |
| Seller org DisplayName | PARTIAL | Party Organization; dashboard read; no update API |
| Seller contact/business | MISSING | Need Party org operational fields + API |
| Locale preference | PARTIAL | Cookie only |
| Customer preferences beyond locale | MISSING | Prefer hide |
| Admin operator profile | MISSING | /admin/settings unavailable shell |
| Notification preferences | DEFERRED | Inbox LIVE; prefs hide |
| Password/security | PARTIAL | Identity password-change exists; settings UI hidden |
| Avatar/logo/media | MISSING | No Media module; hide |
| Fake save forms | Absent (honest) | Vendor dashboard-backed read-only |

Permissions absent: seller.settings.view / seller.settings.manage
