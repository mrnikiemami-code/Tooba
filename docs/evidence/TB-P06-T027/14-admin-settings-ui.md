# 14 — Admin settings UI

## Route
`/admin/settings`

## Behavior
- Replaces unavailable shell with operator profile form:
  - DisplayName, FirstName, LastName, Bio
- Locale preference tab (Host + cookie)
- **No** fake global platform switches
- `GET/PUT /v1/admin/operator/profile`
- `GET/PUT /v1/admin/operator/preferences`
- Uses `X-Tooba-Dev-Actor-User-Id` via admin-api actor pattern

## Files
- `app/admin/settings/page.tsx`
- `app/admin/operator-settings-api.ts`
- `app/admin/admin-shell.tsx` — settings `live: true`, removed from `ADMIN_DEFERRED_NAV_HREFS`

## Preview
`http://127.0.0.1:3000/admin/settings`

Actor seeded from `/v1/admin/dev-context` into `tooba.adminActorUserId`.
