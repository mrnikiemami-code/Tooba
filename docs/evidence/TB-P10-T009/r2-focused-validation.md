# TB-P10-T009-R2 — Focused validation

- Admin appearance + preview + skin + theme: 10 pass (plus recovery 4)
- `test:storefront` 67
- `test:critical-storefront` 18
- invalid skin live PUT 400
- `git diff --check` clean on task files
- Host rebuild skipped (live Tooba.Host lock)
- full `tsc` not required-green; pre-existing Admin/grid failures not repaired
