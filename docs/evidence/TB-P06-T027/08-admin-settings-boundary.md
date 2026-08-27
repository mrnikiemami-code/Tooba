# Admin settings boundary

- Implemented: own operator profile + locale preference only.
- `GET/PUT /v1/admin/operator/profile` — `OperatorProfile` module.
- `GET/PUT /v1/admin/operator/preferences` — `UserPreference` module.
- Auth: `AdminPanelAccess.RequireAuthorizedAsync` (tenant#view).
- **No** generic platform key/value settings store.
- Platform config remains owned by existing modules only.
