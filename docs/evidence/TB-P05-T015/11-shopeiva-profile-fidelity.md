# 11 — Shopeiva Profile Fidelity

Task: `TB-P05-T015`

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
