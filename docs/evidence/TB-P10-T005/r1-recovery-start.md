# TB-P10-T005-R1 — Recovery start

| Field | Value |
| --- | --- |
| Phase | P10 |
| Last Architect-accepted (canonical) | TB-P10-T004-R24-R1-R4 |
| Last Architect-accepted (stale SoT on disk) | TB-P10-T004-R24 |
| Last Implementation | TB-P10-T005 |
| Current Repair | TB-P10-T005-R1 |
| Worker | tooba-worker-01 |
| branch | main |
| HEAD | 92f9f4657678e7ffdcfed5a7bcfc525f0a93f79b |
| origin/main | 92f9f4657678e7ffdcfed5a7bcfc525f0a93f79b |
| HEAD==origin/main | yes |
| USER_VISUAL_ACCEPTED | NO |
| no TB-P10-T006 | yes |

`git status --short` (task-owned clean; unrelated local login/account UX preserved, not listed as recovery conflict):

```text
 M src/frontend/app/login/storefront-login.guard.test.ts
 M src/frontend/app/login/storefront-login.tsx
 M src/frontend/app/storefront/storefront-account-menu.tsx
 M src/frontend/app/storefront/storefront-cart-ui.guard.test.ts
 M src/frontend/lib/auth/login-return-to.test.ts
 M src/frontend/lib/auth/login-return-to.ts
```

Host before recycle: `Tooba.Host` PID 18068, started 2026-09-13 17:41:13, listening `:5088` from `src/backend/Host/Tooba.Host/bin/Debug/net8.0` (pre-T005 process). FE `:3000` node PID 32536 listening.

Catalog appearance migration `20260914010000_AddStoreAppearanceSettings` exists in repo; live Host had not recycled, so `/v1/storefront/appearance` was 404 and table apply was unproven.
