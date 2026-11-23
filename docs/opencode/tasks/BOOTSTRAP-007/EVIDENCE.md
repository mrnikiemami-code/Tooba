# TOOBA-OPENCODE-WORKTREE-BOOTSTRAP-007 — Evidence

Date: 2026-11-23
Mode: INSPECT → SAFE SYNC → PUBLISH → VERIFY
Repo: mrnikiemami-code/Tooba
Product branch: main
OpenCode worktree branch: opencode/iso-agents-002 (product code identical to main)

## Step 1 — Git inspection (before changes)
- remote: origin https://github.com/mrnikiemami-code/Tooba.git
- main worktree: D:/Users/User/source/repos/SarvNewVer @ main (was ahead of origin/main by 5 commits, all OpenCode docs)
- OpenCode worktree: D:/Users/User/source/repos/SarvNewVer-opencode-iso-002 @ opencode/iso-agents-002
- No active merge/rebase/cherry-pick (rebase-merge, rebase-apply, MERGE_HEAD, CHERRY_PICK_HEAD all absent; a stale REBASE_HEAD leftover only).
- main worktree status: clean except untracked scratch (concise-output-006 smoke logs, docs/opencode/optimization/); not committed.
- Unpushed-on-main commits (pre-existing, doc/.opencode only): 32ce31de1, 0a58b6d9d, f1ebd7c9f, da551de00, 7d0a17972.
- origin tip before publish: 17b9680f4. Checked diff of the 5 prepended commits: only .opencode/** and docs/opencode/** touched; no src/, no root AGENTS.md, no .cursor; secret scan clean; 50 files / 1304 insertions (small).

## Step 2 — Versioned OpenCode rules on main (.opencode/)
Created on main (commit 8c5ce7400):
- .opencode/AGENTS.md (short loader; paths → .opencode/iso-rules/)
- .opencode/iso-rules/01-architecture.md
- .opencode/iso-rules/02-security-permissions.md
- .opencode/iso-rules/03-openspec.md
- .opencode/iso-rules/04-opencode-acceptance.md
- .opencode/iso-rules/05-task-layer.md (pack refs → 06-backend.md / 07-frontend.md / 08-amsc.md)
- .opencode/iso-rules/06-backend.md
- .opencode/iso-rules/07-frontend.md
- .opencode/iso-rules/08-amsc.md
- .opencode/setup/README.md
- .opencode/setup/setup-worktree.ps1
No contradictory duplicate: main root AGENTS.md is the single historical file; the short loader lives only at .opencode/AGENTS.md and is activated only inside an isolated worktree.

## Step 3 — Portable setup script
.opencode/setup/setup-worktree.ps1: resolves repo root, creates/reuses one worktree, reads rules from .opencode/, activates short AGENTS.md only in the isolated worktree, asserts root AGENTS.md SHA-256 unchanged and .cursor status unchanged. Idempotent, $ErrorActionPreference=Stop, parse-checked OK.

## Step 4 — Product code sync
git diff --stat origin/main opencode/iso-agents-002 -- src .cursor host → empty (product code already identical).
No merge/rebase performed (would conflict on intentionally-divergent root AGENTS.md; forbidden by task). Iso branch keeps its short AGENTS.md (blob a444451…) by design.

## Step 5 — Architecture rules confirmed (encoded in .opencode/iso-rules)
- Backend → tooba-backend-feature (06); Frontend → tooba-frontend-feature (07); AMSC explicit-only (08).
- Delivery method TOOBA-OPENCODE-DELIVERY-V1@1.3.0 (04).
- Schema/architecture change only with authorization; bugfix within task scope with tests; one bounded repair round.
- Full evidence in file; chat result ≤ 8 lines.

## Step 6 — Publish & verify
- Pushed main: 17b9680f4..8c5ce7400 (fast-forward; 5 pre-existing doc-only commits + rules commit).
- origin/main = 8c5ce74000af5813e2703c33ac3bd08114b18e14
- .opencode/ on origin/main verified present (AGENTS.md, iso-rules 01–08, setup/README.md, setup/setup-worktree.ps1, plus existing opencode.json + instructions).
- Root AGENTS.md blob on origin/main = 7b03e7c58a8c9b0fbaa16358e069dc205fae2196 (unchanged from before).
- diff 17b9680f4..origin/main for AGENTS.md/.cursor/src/host = empty (Cursor + AMSC + product code preserved).
- Push-set top-level paths: .opencode (13), docs (48) only.
