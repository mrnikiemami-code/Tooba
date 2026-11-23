# REPORT — TOOBA-OPENCODE-ISOLATED-WORKSPACE-002

**Gate:** `STOP_FOR_ARCHITECT`  
**Decision requested:** conditional activation of isolated OpenCode worktree (do **not** replace main workspace yet)

## 0. Git remote identity

| Field | Value |
|---|---|
| Working copy audited | `D:\Users\User\source\repos\SarvNewVer` |
| `origin` (actual) | `https://github.com/mrnikiemami-code/Tooba.git` |
| Typo note | Prior draft string `mrnikami-code` was a spelling error; live `git remote -v` is `mrnikiemami-code`. Same Tooba repo — not a different remote. |
| HEAD at measurement | `394cc0bdb` |
| Main `AGENTS.md` | **896,347 bytes** — SHA256 `06DC5A5FEAC45958667BB8F7DE0174BAE478B11F2A7F806C492E33BC6339F090` (**unchanged**) |

## 1. Untouched guarantees

| Asset | Status |
|---|---|
| Main `AGENTS.md` | Untouched |
| `.cursor/**`, `src/frontend/**`, `src/backend/**` | Untouched |
| Task 286 folder / Desktop OpenCode | Untouched (Desktop PID 46632 left running) |
| Product commits / push | None |

## 2. Isolated worktree created (reversible)

| Item | Value |
|---|---|
| Path | `D:\Users\User\source\repos\SarvNewVer-opencode-iso-002` |
| Branch | `opencode/iso-agents-002` (local only; not pushed) |
| Base SHA | `394cc0bdb` (same as main HEAD) |
| Short `AGENTS.md` | **1,190 bytes** layered loader |
| Rule packs | `docs/opencode/iso-rules/01..05-*.md` (architecture, security/permissions, OpenSpec, OpenCode acceptance, task-layer) |
| Project config | `.opencode/opencode.json` (model pin + tool_output/compaction; edit denied for analysis) |

Rules were **not deleted**: full assurance / OpenCode method docs remain on disk inside the worktree; always-on load is a short pack + mandatory `read` of the five layer files. Historical AGENTS dump stays on `main` only.

## 3. Measurement method

- Model: `metisdeepseek/deepseek-v4-flash` (unchanged)
- Scenarios: Backend contract analysis + Frontend component analysis (**not** `say only OK`)
- Each run required a `## RulesApplied` + `## Compliance` section
- Iso runs additionally required reading all five rule packs before the source file
- Harness: `docs/opencode/isolated-workspace/measure-one.ps1` (file redirects; avoids stdout pipe deadlock)

## 4. Results — tokens (measured)

| Run | steps | tools | incrementalInput | output | reasoning | cacheReadPeak | **lastStepTotal (context)** | RulesApplied | Compliance |
|---|---:|---:|---:|---:|---:|---:|---:|---|---|
| main-backend (cold) | 2 | 1 | **257,629** | 768 | 296 | 259,072 | **260,377** | Yes | Yes |
| main-frontend (warm cache) | 2 | 1 | 706 | 601 | 465 | 259,200 | **260,516** | Yes | Yes |
| iso-backend | 3 | 6 | **10,490** | 868 | 228 | 12,160 | **13,139** | Yes (cited packs) | Yes |
| iso-frontend | 3 | 6 | **2,038** | 888 | 7 | 12,160 | **13,079** | Yes (cited packs) | Yes |

### Headline reductions

| Comparison | Main | Iso | Absolute Δ | % |
|---|---:|---:|---:|---:|
| Context size (backend `lastStepTotal`) | 260,377 | 13,139 | −247,238 | **−94.95%** |
| Cold incremental input (backend) | 257,629 | 10,490 | −247,139 | **−95.93%** |
| Context size (frontend `lastStepTotal`) | 260,516 | 13,079 | −247,437 | **−94.98%** |

Currency cost: **NOT_MEASURED** (no provider invoice/rate available).

### Quality / rules

- Iso runs explicitly `read` all five packs + the target source (6 tool calls).
- Answers named module boundaries, authz/write-path risk, OpenSpec “no change loaded”, and “Worker PASS ≠ Architect ACCEPT”.
- Main runs also produced RulesApplied/Compliance (drawing from huge AGENTS / cursor skill references).
- Iso CLI exit code was `1` due to post-run `EPIPE` on stderr after JSONL was fully written; token/text artifacts are complete and used as evidence. Treat exit code as harness noise, not missing proof.

## 5. Wall-clock

| Phase | Time |
|---|---|
| Measurement window (`TASK-START` → `TASK-END`) | **9.83 minutes** (03:52:39 → 04:02:29 +03:30) |
| Includes | main FE/BE + iso retry after one hung pipe (Desktop never killed) |

## 6. Activation proposal (awaiting architect)

**Proposal:** approve optional OpenCode worker path using worktree  
`D:\Users\User\source\repos\SarvNewVer-opencode-iso-002` (`opencode/iso-agents-002`) for analysis/implementation sessions that currently pay ~260k context from root AGENTS.

**Do not** switch default/main workspace until architect confirms:
1. Always-on pack content is complete enough (architecture, security, OpenSpec, OpenCode acceptance, task layer).
2. Task-layer loader is mandatory for issued OpenCode tasks (read TASK.md / matrix / openspec change).
3. Main `AGENTS.md` remains canonical archive on `main`.

## 7. Rollback (no product damage)

```powershell
cd D:\Users\User\source\repos\SarvNewVer
git worktree remove --force D:\Users\User\source\repos\SarvNewVer-opencode-iso-002
git branch -D opencode/iso-agents-002
# optional: remove report-only docs
# Remove-Item -Recurse -Force .\docs\opencode\isolated-workspace
```

Main `AGENTS.md` hash must remain `06DC5A5F…339F090`.

## 8. Artifacts

- Report: this file
- Machine metrics: `docs/opencode/isolated-workspace/runs/*.meta.json` + `*.jsonl`
- Worktree rule packs: `…-opencode-iso-002/docs/opencode/iso-rules/*`

## 9. STOP

One test round completed. No chained tasks started.  
**STOP_FOR_ARCHITECT**
