# CLOSURE — TOOBA-OPENCODE-ISOLATION-CLOSURE-003

**Mode:** VERIFY → PUBLISH EVIDENCE → CLOSE  
**Gate:** `STOP_FOR_ARCHITECT`  
**Parent evidence:** TOOBA-OPENCODE-ISOLATED-WORKSPACE-002 (no benchmark re-run)

## 1. Repository identity

| Check | Result |
|---|---|
| `git remote -v` | `https://github.com/mrnikiemami-code/Tooba.git` |
| Draft typo in 002 | `mrnikami-code` (missing `ema`) — **same repo**, not a fork |
| Closure HEAD before commit | `17b9680f482ec89fd012c3e478199c489e66079c` (`main`) |
| Measurement base (002) | worktree `394cc0bdb` on `opencode/iso-agents-002` |
| Main `AGENTS.md` | 896347 B / SHA256 `06DC5A5FEAC45958667BB8F7DE0174BAE478B11F2A7F806C492E33BC6339F090` unchanged |

## 2. Rules coverage (iso always-on + referenced SoT)

| Requirement | Verdict | Where |
|---|---|---|
| Architecture / FE·BE module boundary | PASS | `iso-rules/01-architecture.md` + Design System / Contracts non-invention |
| Security & permissions | PASS | `iso-rules/02-security-permissions.md` |
| OpenSpec | PASS | `iso-rules/03-openspec.md` |
| OpenCode / architect acceptance | PASS | `iso-rules/04-opencode-acceptance.md` → `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` |
| Impeccable | PASS (by SoT reference) | Same delivery method (Impeccable critique mandatory on UI change); not deleted; short AGENTS points to that SoT |
| Task-specific layer | PASS | `iso-rules/05-task-layer.md` |
| Rules deleted? | No | Historical AGENTS remains on `main` only |

Snapshot of packs copied byte-identical into this evidence folder: `docs/opencode/isolated-workspace/iso-rules/`.

## 3. Token reduction (002 numbers only — not recomputed)

| Metric | Main | Iso | Reduction |
|---|---:|---:|---:|
| Backend `lastStepTotal` | 260377 | 13139 | **−94.95%** |
| Backend cold incremental input | 257629 | 10490 | **−95.93%** |
| Frontend `lastStepTotal` | 260516 | 13079 | **−94.98%** |

Meta cross-check (same artifacts): main-backend input 257629; iso-backend lastStepTotal 13139 / input 10490; iso-frontend lastStepTotal 13079. Cost: NOT_MEASURED / NOT_AVAILABLE.

## 4. Publish scope

- Committed under `docs/opencode/isolated-workspace/` only.
- Not touched: Frontend, Backend, main `AGENTS.md`, Cursor, domain architecture, OpenCode Desktop / task 286, iso→main merge.
- Untracked `.opencode/` on main left out of this commit (avoid changing main OpenCode config).

## 5. Optional select / rollback

See `WORKSPACE-SELECT-AND-ROLLBACK.md`.

## 6. STOP

Closure of the token-reduction evidence project. No chained tasks.  
**STOP_FOR_ARCHITECT**
