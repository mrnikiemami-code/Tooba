# 01 — Runtime before migration work (TB-P06-T002)

Recorded at BRIDGE-WAKE claim / task start (predecessor `941399a48e01a2c2517dca59fe8c574abe5cf69e`).

| Runtime | Status at claim | Notes |
|---|---|---|
| PostgreSQL | available | local dev instance `:5432` (used by Development appsettings) |
| Tooba Backend | not running | health probe to `:5088` failed before implementation |
| Tooba Frontend | not running | home probe to `:3000` failed before implementation |

Recovery checks:

| Check | Result |
|---|---|
| branch | `main` |
| HEAD | `941399a48e01a2c2517dca59fe8c574abe5cf69e` |
| origin/main | `941399a48e01a2c2517dca59fe8c574abe5cf69e` |
| tracked tree | clean (only untracked dev logs) |

Rationale: migration-runner work is backend/ops-only; no UI edits expected. Runtimes restarted after validation for Part R evidence (`13-final-runtime-preview.md`).
