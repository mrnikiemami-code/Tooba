# TB-P10-T006 — Permissions

Reuses `AdminPanelAccess.RequireAuthorizedAsync` on GET and PUT. No new permission registry entry.

Unauthorized / missing actor → 401/403. FE maps those to denied and hides/disables via existing `profile.editable` / denied state.

Store scope is backend commerce context (`tenant:{id}`). Admin of Store A cannot address Store B through this API; there is no StoreId in the write body.

Source: `StoreAppearanceSettingsEndpoints.cs`. Runtime: PUT without `X-Tooba-Dev-Actor-User-Id` is rejected; valid actor writes only the current Host store.
