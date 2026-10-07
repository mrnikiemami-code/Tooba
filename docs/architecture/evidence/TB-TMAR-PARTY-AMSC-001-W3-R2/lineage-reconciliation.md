# TB-TMAR-PARTY-AMSC-001-W3-R2 — Lineage reconciliation

All parent links verified with `git log --format='%H %P'` at the precheck HEAD `a75e3bf4390c7950ce8fa0e9462b786aecefe9c1`:

| Commit | Parent | Matches task expectation |
|---|---|---|
| `2477bbb30224c0976b7b02a3d12be9e1b72248db` (W0) | (root of this chain) | ✓ |
| `29012df08590550d3171722895d988bd59e3c2fa` (W1 impl) | `2477bbb3…` | ✓ 29012df0<-2477bbb3 |
| `ee9ba997ccc2c70009a71dc242a983335d098a19` (W1 metadata) | `29012df0…` | ✓ ee9ba997<-29012df0 |
| `ffff7100bc119a76c472ba77e27a658b1f18f129` (W2 impl) | `ee9ba997…` | ✓ ffff7100<-ee9ba997 |
| `f0621ca6dd2ba7abb4e2feaa1fbdf5ce2c8eb711` (W2 metadata) | `ffff7100…` | ✓ f0621ca6<-ffff7100 |
| `d1cc2f480619ce1ec68cd730650018883684e6c7` (W3 certify) | `f0621ca6…` | ✓ d1cc2f48<-f0621ca6 |
| `548a7829e3c40b9158589118e521add67eaec6a7` (W3 metadata) | `d1cc2f48…` | ✓ 548a7829<-d1cc2f48 |
| `a75e3bf4390c7950ce8fa0e9462b786aecefe9c1` (W3-R1) | `548a7829…` | ✓ a75e3bf4<-548a7829 |

Starting HEAD confirmed: `HEAD == origin/main == a75e3bf4390c7950ce8fa0e9462b786aecefe9c1` (verified via `git fetch origin main` + `git rev-parse HEAD origin/main`).

## SHA before → after (SoT)

| SoT field | Before | After |
|---|---|---|
| `partyAmsc001W0.commit` | `PENDING_THIS_COMMIT` | `2477bbb3` (+ `commitFull` `2477bbb30224c0976b7b02a3d12be9e1b72248db`) |
| `partyAmsc001W3R1.commit` | (absent) | `a75e3bf4` (+ `commitFull` `a75e3bf4390c7950ce8fa0e9462b786aecefe9c1`) |
| `partyAmsc001W3R2` | (absent) | new block, `state = PARTY_AMSC_001_RECOVERY_CLOSED_RECONCILED`, no self-referential PENDING commit |

`partyAmsc001W3.certifiedCommit` / `acceptedLineage` (W0 `2477bbb3` / W1 `29012df0` / W2 `ffff7100` / W3 `d1cc2f48`) unchanged — W3 authority untouched.

## Metadata commit classification

`ee9ba997`, `f0621ca6`, `548a7829` each changed only `docs/architecture/tmar-current-state.json` + `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` + evidence `.md` files (verified from the W1/W2/W3 commit messages and their recorded file stats: "metadata-only reconciliation, zero production change"). Classification: `METADATA_ONLY_NOT_IMPLEMENTATION_WAVES`.
