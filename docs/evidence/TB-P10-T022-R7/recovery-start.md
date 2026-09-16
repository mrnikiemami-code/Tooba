# TB-P10-T022-R7 — Recovery Start

Recorded: 2026-09-16 (BRIDGE-WAKE claim)

## Git

| Field | Value |
|-------|-------|
| branch | `main` |
| HEAD | `87ed71835284edac81cb0692c63246cf2f31904e` |
| origin/main | `87ed71835284edac81cb0692c63246cf2f31904e` |
| HEAD == origin/main | yes |
| tracked dirty | none (task-owned clean at start) |
| unrelated | abundant `.tmp-*` / evidence leftovers preserved untracked |

Expected task previous HEAD note (`7f6e23c8…`) is an ancestor of current tip (R6 implementation + evidence commits + fashion sample/store preview UX `87ed7183`). No RECOVERY_CONFLICT.

## Claim

- Bridge UUID: `0b585327-51c7-441e-8621-c4cf289038c0`
- Task-ID: `TB-P10-T022-R7`
- Channel: `tooba-main`
- Worker: `tooba-worker-01`

## Current Fashion preview (pre-repair)

| Surface | Path / behavior |
|---------|-----------------|
| iframe | `/template-preview/fashion?source=sample\|store` |
| full page | `/template-preview/fashion/full?source=sample\|store` |
| Admin open | `admin-template-selection-workspace.tsx` → `fashionPreviewSrc` / `fashionFullPageHref` |
| Sample loader | `loadFashionTemplatePreview` → Template Catalog API |
| Store loader | `loadFashionStorePreview` → `/products` + `/categories` + `/brands` (avoids flaky `/home`) |
| Defect | Store Hero/Banner fall back to `FASHION_IMAGES` when media missing |
| Defect | empty BannerShowcase can fall through to home `MIDDLE_BANNERS` |
| Locale | `useLocale()` only; no preview toolbar language from language tables |
| Focal | `MediaAsset` has no FocalPointX/Y; hero/banner use `object-cover` only |

## Host / FE

| Service | Expectation |
|---------|-------------|
| Host | `http://127.0.0.1:5088` |
| FE | `http://127.0.0.1:3000` |
| Bridge | `http://127.0.0.1:17321` healthy |

## Locks preserved

LOCK-SF-001…296 at start; R7 will add LOCK-SF-297…305.
