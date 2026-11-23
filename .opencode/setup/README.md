# Portable OpenCode bootstrap (TOOBA)

Canonical, versioned home for the OpenCode-only instructions used by the TOOBA
workers. Recovery and re-setup on another computer start here.

## What lives here
- `.opencode/AGENTS.md` — the SHORT always-on loader. It is copied into an
  isolated worktree root as `AGENTS.md`; it is **never** committed over the
  main-tree `AGENTS.md`.
- `.opencode/iso-rules/01-architecture.md` … `08-amsc.md` — layered OpenCode
  rules (always-on 01–05, task packs 06–08).
- `.opencode/instructions/concise-chat-output.md` — chat verbosity rules
  (referenced by `.opencode/opencode.json`).
- `.opencode/setup/setup-worktree.ps1` — idempotent portable setup script.

## Why it is safe
The main-tree `AGENTS.md` (full historical), `.cursor/**`, AMSC skills, Host, and
product code are **never** modified by this folder or the setup script. The short
loader is activated only inside a dedicated, isolated git worktree.

## Setup on another computer
```powershell
git clone https://github.com/mrnikiemami-code/Tooba.git
pwsh -File .opencode/setup/setup-worktree.ps1
```

Optional explicit paths:
```powershell
pwsh -File .opencode/setup/setup-worktree.ps1 `
  -RepoRoot     'D:\path\to\Tooba' `
  -WorktreePath 'D:\path\to\Tooba-opencode' `
  -Branch       'opencode/iso' `
  -BaseBranch   'main'
```

## Guarantees
- Idempotent: safe to run repeatedly.
- Creates or reuses one dedicated worktree and one branch.
- Reads OpenCode rules from `.opencode/` only.
- Refuses to write the repository-root `AGENTS.md` (SHA-256 checked before/after)
  or `.cursor/**` (status compared before/after).
- No merge, rebase, reset, stash, or product-code change.

## Naming note
Task packs are `06-backend.md`, `07-frontend.md`, `08-amsc.md` (previously
`*-layer.md`). `05-task-layer.md` references these canonical names.

## Recovery check
After setup, `AGENTS.md` at the repository root must still be the full historical
file, and `.opencode/iso-rules/` must exist with 8 files.
