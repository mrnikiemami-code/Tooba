# Visual Evidence Capture Note

Task: `TB-P05-T015`

Required PNG artifacts:

- `03-profile-desktop-before-save.png`
- `04-profile-desktop-after-save.png`
- `05-profile-validation.png`
- `06-profile-readonly-identity-fields.png`
- `08-profile-dashboard-reflection.png`
- `09-profile-mobile-390x844.png`

Worker session note: captures require local PostgreSQL (`127.0.0.1:5432`) and Host bootstrap identical to prior P05 evidence tasks. Docker/Testcontainers and PostgreSQL service were unavailable in the worker environment during implementation validation.

Capture procedure (Architect/local):

1. Start PostgreSQL with tenant databases from `appsettings.Development.json`.
2. Run Host on `http://127.0.0.1:5088` and frontend on `http://127.0.0.1:3000`.
3. Open `/customer-panel/profile` with dev actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`.
4. Capture desktop 1440×900 before save, validation error state, after successful save, readonly identity fields, dashboard greeting reflection, and mobile 390×844.

Implementation and API binding are complete; PNG files should be captured during Architect review when runtime is available.
