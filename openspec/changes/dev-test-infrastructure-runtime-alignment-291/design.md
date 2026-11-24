# Design — dev-test-infrastructure-runtime-alignment-291

## Ownership
Test-infra lives under `docs/opencode/test-infra/` + `scripts/dev/`; it is not product code.

## Dev runtime contract (single reference)
`DEV-TEST-ENV.json`: backend base/health/provenance, frontend base/readyPath, db container/name/user, discovery
endpoints (seller/admin dev-contexts), offer pages + expected headings, backup path/format, version artifacts
(frontend `.next/BUILD_ID`, backend dll sha).

## One-command verify (delivery proof)
`scripts/dev/verify-dev-alignment.ps1` returns pass only if:
frontend /fa = 200 AND backend /health = 200 AND db reachable AND each changed offer page returns 200 AND its
expected heading text is actually present in the SSR HTML. It also reports git SHA, frontend BUILD_ID, backend dll
SHA, DB migration column presence (287 additive) => front/back/db alignment.

## Discovery
Actor/seller ids come from `/v1/seller/dev-contexts` and `/v1/admin/dev-context` at runtime; harnesses pass ids via
env (see connected-287.cjs TB287_* env), never hardcoded old ids.

## Non-goals
No product/schema/permission change; no weakening of architecture/security; no per-task harness duplication.
