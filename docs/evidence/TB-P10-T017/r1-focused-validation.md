# Focused validation — TB-P10-T017-R1

| Check | Result |
| --- | --- |
| FE appearance / palette / surface-role / theme | 23/23 |
| `npm run test:critical-storefront` | 19/19 (pdp guard includes first-paint / no `useTheme` on toggle) |
| `pdp-structure.guard.test.ts` | 6/6 |
| `storefront-theme.test.ts` | includes `doesNotMatch(/useTheme/)` |
| recovery-staleness.guard.test.mjs | 4/4 (`CURRENT_TASK_ID=TB-P10-T017-R1`) |
| Host StoreAppearance | not rerun (no C# change) |
| `git diff --check` | CRLF warnings only |
| USER_VISUAL_ACCEPTED | NO |
| TB-P10-T018 | not created |
