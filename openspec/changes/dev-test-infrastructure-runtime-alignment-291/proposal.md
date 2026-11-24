# Proposal — dev-test-infrastructure-runtime-alignment-291

## Why
Harnesses and delivery checks were hard-coded to ports/databases/actor ids from past tasks, and "home page 200" was
mistaken for product health. This caused staging/runtime misalignment (running frontend build lacks routes, backend
build differs from branch, old DB/ids) and repeated per-task harness rewrites.

## What Changes
- One source of truth: `docs/opencode/test-infra/DEV-TEST-ENV.json` (bases, db, discovery endpoints, pages, headings).
- One command per environment: `scripts/dev/verify-dev-alignment.ps1` proves frontend build id + reachability, backend
  health+provenance, DB reachability + migration column, and REAL access (heading text) to the changed offer pages.
- Delivery standard (permanent, in `.opencode/AGENTS.md`): every task must prove source commit + runtime frontend
  version, backend version, DB/migration compatibility, and real page access. "Home 200" alone is NOT proof.
- Harnesses discover actors/data at runtime from the configured discovery endpoints; no hardcoded old ports/ids.

## Impact
Docs/scripts + OpenSpec + OpenCode instructions. No product code or schema change.
