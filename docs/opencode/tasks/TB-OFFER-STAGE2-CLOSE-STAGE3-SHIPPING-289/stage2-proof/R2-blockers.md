# TB-289-R2 blockers (2026-11-24, HEAD c31efbe9)

Preserved: CartSnapshotProjector/CartDirectory fixes + C01 strict PASS (not re-run/rewritten).

## C07 (Playwright) — BLOCKED (environment/tooling, no provisioning allowed)
- Browser dir docs/ai/tasks/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283/browser has NO node_modules;
  @playwright/test is not installed (
px --no-install playwright fails: missing package playwright@1.65.1).
- Browsers exist in ms-playwright cache, but the test runner package is absent. Installing it = npm provisioning,
  which this directive forbids. => C07 cannot be executed truthfully without a provisioning step (architect decision).

## C02 / C04 / C05 — require the destructive live-candidate cycle (not executed)
- No in-process .NET harness exists for the 287 16-test matrix; the only harness is .../287/evidence/connected-287.cjs
  which needs a running candidate Host (287+288+Cart fix) pointed at a disposable/dev DB with seller+admin auth.
- C02 = second normal boot on a candidate deployment after a controlled Catalog reset (destructive on tooba_alpha) +
  idempotence SQL checks.
- C05 = 287 VERIFY realOk=true, which requires a clean 287 connected run producing a passing connected-287.json.
- These require the same single destructive dev cycle (deploy candidate -> reset -> boot1 -> boot2 -> run harness ->
  restore). Not executed in this turn; nothing fabricated.

## Env
5088/3000 healthy; tooba_alpha baseline; backup retained (SHA256 4839D664...07FB9C).
