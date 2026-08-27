# Settings development seed

- Entry: `SettingsFoundationDevelopmentSeed.ApplyAsync` from `ProductWorkspaceDevelopmentBootstrap` (Dev-only).
- Seeds:
  1. Seller org profile fields on «فروشگاه آرمان»
  2. UserPreference locale `fa` for storefront guest `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`
  3. UserPreference locale `fa` for admin demo actor (from `AdminDevActorBootstrap.Snapshot`)
  4. OperatorProfile for platform admin demo actor
- Idempotent; Production never calls.
- Employee role left without settings.manage.
