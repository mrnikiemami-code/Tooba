# TB-P07-T001-R3 — Saved views

## Persistence owner
New `user_preference.ui_preferences` table (not locale `user_preferences`):
- `preference_id`, `actor_user_id`, `key`, `json_payload`, `updated_at`
- unique `(actor_user_id, key)`

Migration: `20260828020000_AddUiPreferences`

## Host API
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/v1/admin/ui-preferences/{key}` | `AdminPanelAccess` |
| PUT | `/v1/admin/ui-preferences/{key}` body `{ json }` | same |

Missing key returns `{ key, json: null, updatedAt: null }`.

## Frontend adapter
`createHostSavedViewStore(preferenceKey)` implements `SavedViewStore` (`list` / `save` / `remove`).
Payload shape: `{ views: SavedGridView[] }`.

| Screen | Key |
| --- | --- |
| Products | `grid.admin.products` |
| Orders | `grid.admin.orders` |

Network failures degrade gracefully (empty list / keep local cache) so the grid remains usable.

## Files
- Domain/App/Infra: `UiPreference`, `IUiPreferenceDirectory`, `UiPreferenceDirectory`, DbContext + migration
- Host: `Preferences/UiPreferenceEndpoints.cs`, `Program.cs` `MapUiPreferenceEndpoints`
- FE: `src/frontend/app/admin/saved-view-store.ts`
