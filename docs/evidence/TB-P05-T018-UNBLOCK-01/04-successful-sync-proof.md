# 04 — Successful Sync Proof

## Method that succeeded

1. Soft-reset / rebuild from predecessor `11b7ee9` after PNG optimization (~5.8 MiB total).
2. Split transport into three fast-forward pushes with `http.version=HTTP/1.1`:
   - **PUSH1** `f77cc4a` — T018 code + markdown evidence (no PNGs) → `origin/main`
   - **PUSH2** `6a7dc13` — PNG batch 1 (9 files) → `origin/main`
   - **PUSH3** `8482124` — PNG batch 2 (7 files) → `origin/main`

## Final equality

| Item | Value |
| --- | --- |
| HEAD | `84821249a82f11a5aaf766ff94979280b2e212ef` |
| origin/main | `84821249a82f11a5aaf766ff94979280b2e212ef` |
| equal | yes |
| force push | no |
| TLS weakened | no |

## Working tree

Tracked tree clean and synchronized with `origin/main`. Untracked local runtime logs only (`host-dev*.log`, `next-dev*.log`) — not part of T018 / not committed.
