# TB-P10-T022-R8 — Recovery Start

Recorded: 2026-09-16 (BRIDGE-WAKE claim)

## Git

| Field | Value |
|-------|-------|
| branch | `main` |
| HEAD | `29be740ed72656f37b477467bda90969eae78c99` |
| origin/main | `29be740ed72656f37b477467bda90969eae78c99` |
| HEAD == origin/main | yes |
| tracked dirty | none (task-owned clean at start) |
| unrelated | abundant `.tmp-*` leftovers + stash `unrelated-user-r8-preflight` preserved |

Expected previous HEAD matches tip. No RECOVERY_CONFLICT.

## Claim

- Bridge UUID: `0adfee69-79bb-4650-9c4b-e1ba766c5661`
- Task-ID: `TB-P10-T022-R8`
- Channel: `tooba-main`
- Worker: `tooba-worker-01`

## Pre-repair state (R7)

| Surface | Behavior |
|---------|----------|
| Store empty sections | `PreviewPlaceholderSurface` generic dashed boxes |
| Sample mode | Template Catalog Fashion persisted data |
| Banner Store empty | `previewPlaceholder` + no Template Catalog fallback |
| Variant registry | No `previewMinItems` / `previewTargetItems` |
| Published path | `preview` false → no placeholders |

## Host / FE

| Service | Status at start |
|---------|-----------------|
| Host :5088 | up (HTTP 200) |
| FE :3000 | down |

## Goal

Replace Store-preview generic placeholders with preview-only fake fill rendered through the same production Section/Variant components.
