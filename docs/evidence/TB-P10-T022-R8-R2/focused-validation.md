# Focused Validation — TB-P10-T022-R8-R2

- `admin-builder-ux-r8.guard.test.ts` — PASS (14)
- `npm run test:critical-storefront` — PASS (20)
- `node --test docs/ai/recovery-staleness.guard.test.mjs` — PASS (4)
- `git diff --check` — CLEAN (CRLF warnings only)

Isolation guards covered:

- Store Preview fake fill uses `/images/preview-placeholder/` only
- Zero Template Catalog entity/media IDs/paths in fake items
- `previewSource === PreviewFake` in-memory marker
- Sample mode still Template Fashion media
- Published Storefront fill disabled
- Locale resources for Preview-Fake copy
- Locks LOCK-SF-312…315 registered
