# 12 — Customer settings UI

## Route
`/customer-panel/settings`

## Behavior
- Profile link section → `/customer-panel/profile` (LIVE)
- Tabbed Shopeiva-derived shell (`#2563EB` accent):
  - **زبان** LIVE: load locale from `GET /api/customer/preferences` (→ `/v1/customer/preferences`) if present, else cookie; on select write cookie **and** `PUT` Host preference
  - **امنیت** unavailable dashed copy (no fake password toggles)
  - **اطلاعیه‌ها** unavailable dashed copy (no fake notification toggles)
- Appearance/theme tab **omitted** (no fake theme persistence)

## Files
- `app/customer-panel/settings/page.tsx`
- `app/customer-panel/customer-preferences-api.ts`
- Existing catch-all BFF: `app/api/customer/[...path]/route.ts`

## Preview
`http://127.0.0.1:3000/customer-panel/settings`
