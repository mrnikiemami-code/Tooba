# 11 — Shopeiva Profile Fidelity

Task: `TB-P05-T015` / repair `TB-P05-T015-R1`

Reference: purchased `ProfileForm` (`reference/shopeiva/.../profileForm.jsx`).

Preserved in Tooba `/customer-panel/profile`:

- card shell with header icon + title «اطلاعات پروفایل»
- avatar block with camera affordance (disabled honestly)
- combined name field spanning full width on desktop
- birth date field row
- email + mobile paired row (read-only styling)
- national code row (read-only honest state)
- address section (redirect to AddressBook instead of fake textarea persistence)
- bio textarea with 200-char counter
- primary save button with spinner state
- responsive `md:grid-cols-2` grouping

Intentional minimal deltas (allowed):

- Tooba blue `#2563EB` instead of Shopeiva red
- Persian validation/error messages bound to live backend
- read-only locks on Identity-owned fields

Not added: redesign, new account shell, or separate profile product UI.

## Live visual evidence (R1)

Captured on 2026-08-26 from Host `http://127.0.0.1:5088` and Next `http://127.0.0.1:3000` via `scripts/capture-t015-evidence.mjs` (Chrome CDP headless, dev actor `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`):

| File | Viewport | Proof |
| --- | --- | --- |
| `03-profile-desktop-before-save.png` | 1440×900 | seeded profile before edit |
| `05-profile-validation.png` | 1440×900 | live Persian/length validation on name |
| `04-profile-desktop-after-save.png` | 1440×900 | successful save of editable fields |
| `06-profile-readonly-identity-fields.png` | 1440×900 | email/mobile/nationalCode locked |
| `08-profile-dashboard-reflection.png` | 1440×900 | dashboard greeting reflects saved `displayName` |
| `09-profile-mobile-390x844.png` | 390×844 | responsive mobile profile fidelity |
