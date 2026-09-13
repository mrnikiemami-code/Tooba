# TB-P10-T004-R24-R1-R2 — Browser contexts

Two isolated Playwright Chromium contexts (separate cookie jars, no shared storage).

| Context | Auth | Cookies | Purpose |
| --- | --- | --- | --- |
| Customer | Development OTP UI `09111111111` / `123456` on `/fa/login` | `tooba_session` only in this context | Shipping → Payment → Customer Order |
| Admin | Existing Development admin seam `prepareAdminDevActor` / `tooba.adminActorUserId` = `01a036c2-970e-7000-8eb7-94bf5cc2d8db` | none of the customer session | `/admin/orders/{id}` |

Not done:

- overwrite customer cookies with Admin
- reuse the customer `tooba_session` on Admin
- change Host/FE auth architecture
- screenshot-only header injection

Script: `_r24-r1-r2-visual.mjs`.
