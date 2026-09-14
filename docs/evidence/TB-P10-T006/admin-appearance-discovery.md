# TB-P10-T006 — Admin appearance discovery

Canonical Store settings remain `/admin/settings` (`src/frontend/app/admin/settings/page.tsx`).

Existing tabs: profile, locale, quantity, holds, identity, limits. T006 adds `appearance` / «ظاهر» on the same tab strip. No parallel settings shell.

Save pattern reused: `busy` / `error` / `success` / Cancel resets draft. Appearance uses `AdminAppearanceSettingsForm` + `loadAppearanceSettings` / `saveAppearanceSettings`.

Authorization: `AdminPanelAccess.RequireAuthorizedAsync` (same as other Store Admin settings). FE `prepareAdminDevActor` + `X-Tooba-Dev-Actor-User-Id`. Backend remains authoritative.

API convention: `/v1/admin/settings/appearance` GET/PUT, JSON camelCase, `paletteKey` only on write.

No prior write path for PaletteKey (T005 was read-only storefront projection). No ThemeMode control exposed. No toast-only success; inline `setSuccess` matches other settings tabs.
