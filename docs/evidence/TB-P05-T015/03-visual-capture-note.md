# Visual Evidence — Profile (R1 complete)

Task: `TB-P05-T015` / repair `TB-P05-T015-R1`

All mandatory PNG artifacts are present under `docs/evidence/TB-P05-T015/`:

- `03-profile-desktop-before-save.png`
- `04-profile-desktop-after-save.png`
- `05-profile-validation.png`
- `06-profile-readonly-identity-fields.png`
- `08-profile-dashboard-reflection.png`
- `09-profile-mobile-390x844.png`

Capture procedure: `scripts/capture-t015-evidence.mjs` against live Development Host + Next dev server with PostgreSQL on `127.0.0.1:5432` and controlled dev actor header seam.

Runtime API probe: `_api-probe-r1.json` (GET/PUT profile, dashboard reflection, actor B isolation).
