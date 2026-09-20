# Legacy failures — TB-TMAR-FE-F1

| Suite | Before (FE-BASELINE) | After (FE-F1) |
| --- | --- | --- |
| critical-storefront `bg-white` in `storefront-landing-blocks.tsx:285` | FAIL | FAIL (untouched; not in slice) |
| `npm run typecheck` legacy admin/grid errors | FAIL | FAIL (no new errors introduced by slice; not broadly fixed) |

No failure suppression. Slice/architecture suites green.
