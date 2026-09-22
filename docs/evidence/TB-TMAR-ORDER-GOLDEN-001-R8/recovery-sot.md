# Recovery SoT — TB-TMAR-ORDER-GOLDEN-001-R8

## Claim

- Task-ID: `TB-TMAR-ORDER-GOLDEN-001-R8`
- Claim-Id: `82d1a199-12fd-4907-ba10-796408944766`
- Channel: `tooba-main`
- HEAD at start: `8baca44a84bb5964030ce605aefc4f6cf4ef2eba` (== `origin/main`)

## Outcome slice

- `RESERVATION_CYCLE_POLICY_RETRY_EXPIRY_R8`
- Host `ReservationCycleCoordinator` + `ReservationCyclePolicyResolver` deleted
- Order owns policy merge + retry orchestration + unpaid expiry reconciliation
- Catalog hold overrides exposed via Contracts reader
- Host unpaid expiry worker is shell-only

## Module state

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R9` (not started)
- Gate: `ORDER_GOLDEN_REPAIR_REQUIRED`

## Durable docs synced

- `docs/architecture/tmar-current-state.json`
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`
- `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md`
- R7 `host-order-reference-inventory.json` updated for R8 removals (historical R7 audit md preserved)

## Worker note

- Do **not** commit / push / Bridge Result from this Worker run unless Architect instructs.
- Do **not** start R9 / CustomerPanel / SellerPanel / Admin grids / Checkout W6 / frontend.
