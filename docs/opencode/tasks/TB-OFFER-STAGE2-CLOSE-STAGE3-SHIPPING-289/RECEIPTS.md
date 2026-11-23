# RECEIPTS — TB-OFFER-STAGE2-CLOSE-STAGE3-SHIPPING-289

## Ancestry / preserved work
- origin/main at issuance read: f922eaa0923f01b013fe0f2fad667ff364d6fc4a.
- 287 commit ae7d7dbba265a8b58dd32c169f7af0f42f15ef4c; 288 commit bb6d94caabdcbcd55cdbbe4a245ca61e7c6224a9
  (worker branch `opencode/iso-agents-002`). 287/288 code and evidence preserved; not restarted.

## Safe Stage-2 evidence captured this task (no destructive run)
- C05: `stage2-proof/C05-verify-287.txt` — 287 VERIFY: mutations all REJECTED; `realOk=false`, decision BLOCKED_EVIDENCE.
- C06: `stage2-proof/C06-observation.txt` — 5088 `/health`=200, 3000 `/fa`=200; backup file + SHA256 preserved.
- C08: source scope reviewed (287 = Offer + additive nullable migration; 288 = Offer seed gateway + test; no root
  AGENTS/.cursor/Host touch).
- Development backup (owner-authorized, preserved, not in git):
  `...\dev-db-backups\tooba_alpha_288backup_20261123_133446.dump`, 3,091,575 B,
  SHA256 4839D6649C4E4066A404FA73EA95BFF60AE67343EEA4A74BF81F2C582B07FB9C, pg_dump exit 0, pg_restore --list exit 0.

## Stage-3 design artifacts (design only)
- `stage3-design/STAGE3-ARCHITECTURE.md`, `STAGE3-DECISION-GATES.md`, `STAGE3-ACCEPTANCE-MATRIX.json`.
- OpenSpec `openspec/changes/offer-stage3-shipping-assignment-289/**`:
  `openspec validate offer-stage3-shipping-assignment-289 --strict` → `Change ... is valid`.

## Boundaries honored
No Stage-3 product code; no Stage-2 acceptance claimed; no root AGENTS/.cursor/Host/Inventory/API/Pricing change; no new
DB/Host; no provisioning loop; no 287 migration deployed to dev DB. Canonical 5088/3000 healthy.

## Cost / time
NOT_AVAILABLE. `STOP_FOR_ARCHITECT`.
