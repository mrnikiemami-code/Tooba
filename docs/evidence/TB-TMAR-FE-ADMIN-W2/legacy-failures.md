# Legacy failures — TB-TMAR-FE-ADMIN-W2

Pre-existing (unchanged; not suppressed):

- critical-storefront / component-compliance `bg-white` failure
- legacy typecheck / PAGE_SIZES characterization drift
- `admin-api.test.ts` “sends development actor header…” → `host-unreachable` under native ESM `export {…} from` (no local binding); present on pre-W2 tip
- Host inventory evidence count drift (1755 vs scan 1751) outside this slice

No new failure signatures introduced by admin-reviews migration.
Slice validation: architecture 11/11 + characterization 6/6 + FE-SIZE/FOLDER green.
