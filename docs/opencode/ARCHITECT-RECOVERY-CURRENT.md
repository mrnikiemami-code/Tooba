# Tooba Architect current recovery — Offer Stage 2 / OpenCode

Updated: 2026-11-23. Source: accessible GitHub main at `1085116c34892f692d14b16050632db02df4592f` before task issuance. **Read live Git state before relying on this snapshot.**

## Authority and current gate
- Canonical repo: `https://github.com/mrnikiemami-code/Tooba` (main).
- OpenCode published rules: `.opencode/AGENTS.md`, `.opencode/iso-rules/01–08`, `.opencode/setup/`; original root `AGENTS.md`, `.cursor/**` and AMSC MUST NOT be changed or overwritten. Worktree `SarvNewVer-opencode-iso-002` retains short root loader; product code sync must be explicitly checked. Do not merge isolated root AGENTS into main.
- OpenCode method `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` v1.3.0, strict OpenSpec for behavior changes and Impeccable only for actual UI design/change. AMSC is NOT default for regular Offer bugfix; use backend feature skill; architecture-specific tasks only if explicitly authorized. One first full matrix, one consolidated repair and final rerun maximum. Preserve real tests; no invented PASS.
- Task 285 code baseline `507d7933a54fe067c240beb6aeb62b63f4b110c0`, Stage 2 prior architect review `docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/ARCHITECT-FINAL-REVIEW.md`.
- Task 286 `docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286/{RESULT.md,RECEIPTS.md}`: V1/V2/V3 PASS, V4 FAIL. Confirmed source `CreateOfferCommand.cs` compares original replay to current mutable SellerOffer state; after edit A→B old A→409 and changed B→201, violating immutable creation intent.
- **New architect-issued task** `docs/opencode/tasks/TB-SELLER-OFFER-IMMUTABLE-CREATE-INTENT-287/TASK.md` and `ACCEPTANCE-MATRIX.json`: implement durable immutable creation intent, test connected against disposable PG, one bounded repair; existing 285/286 evidence untouched. Authorized minimal Offer-owned additive schema source if necessary; NO live application/backfill. Historical unknown creation intents fail closed.
- **Current decision: Offer Stage 2 BLOCKED, NOT ACCEPTED; Stage 3 NOT ISSUED.** Worker READY is not architect acceptance. No automatic next task.
- Chat loss recovery: read this file → current `ARCHITECT-DELIVERY-METHOD.md` → issued 287 TASK/MATRIX → 286 RESULT/RECEIPTS → current source/diff/commits and worker evidence → independently review; never rely on chat summary as authority. When task 287 finishes, update this recovery with exact verified SHAs and architect verdict.
- Cross-team safety: no root AGENTS, .cursor, AMSC, Host, shipping, Pricing/Inventory or unrelated modules changes. One GitHub main and separate OpenCode worktree.
