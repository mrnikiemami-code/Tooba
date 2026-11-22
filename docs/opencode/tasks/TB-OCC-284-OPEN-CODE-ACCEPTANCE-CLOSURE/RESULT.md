# RESULT — TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE

Status: **PASS**
Readiness: **READY_FOR_ARCHITECT_REVIEW**
Decision: **OPEN_CODE_EVIDENCE_PASS**
Architect ACCEPT: **NOT ISSUED**
Owner acceptance: **NOT CLAIMED**

## Scope and decision

This task independently accepted the recorded Stage 1 evidence for TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283 and its connected closure 284. It did not change feature code, schema, data, configuration, Cursor governance, the frozen 283/284 task artifacts, AGENTS.md, the roadmap or the Bridge protocol.

The 284 worker result was `BLOCKED` only because the Cursor `check-readiness.mjs` requires `| REQ-... |` table rows in the issued TASK and the issued 284 TASK has none. That failure is preserved as **KNOWN_FORMAT_INCOMPATIBILITY**. It is not relabelled as product PASS or product FAIL. The OpenCode acceptance uses its own deterministic contract and checker.

Result: **all 7 original REQ rows PASS, 38/38 concrete connected assertions PASS, OpenCode checker PASS, negative self-test PASS.** No material missing proof remains for OpenCode review. The candidate method is prepared as **CANDIDATE — NOT ADOPTED** and is not canonical.

## Evidence identity

- Source product code: `611f97a85ec7c2e464e4f649820992202ca3754c` (unchanged).
- Source evidence commit: `fc40d412aa6d085d3db3f1deac461eb93a10f48d`.
- Current base at execution: `844c23237d19aeabc453f5fb04561657c4dd967a`; origin/main matched before commit.
- Connected evidence: `docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/evidence/connected-284.json`, SHA-256 `0a155bcc2fa5396b8d981e3246036523f70800a3bdf2e02db5c08fb89528afc1`.
- Harness: `connected-284.cjs`, SHA-256 `ebf1f993594684a08bed26d8be9aab87420d2522465cf3f4434597f5a535dc36`.
- Run log: `connected-284.log`, SHA-256 `29454955b7d09a3999d18a0bae42a4c4ba35b38c82eb66c887e32cfb595acf3a`.
- OpenCode checker: `VERIFY-OPEN-CODE.mjs`; output is `verification.log`.
- OpenCode manifest: `EVIDENCE-MANIFEST.json`.
- Independent review: `EVIDENCE-REVIEW.md`.

## Requirement result

| Requirement | OpenCode result | Basis |
|---|---|---|
| REQ-01 SKU-only save preserves positions | PASS | One Offer PATCH, zero price/stock writes, independent GET and SQL show new SKU, price 2,500,000, positions 3/7, reservations 1/2, outbox 27 → 27 |
| REQ-02 Draft and explicit both-limit clear | PASS | Draft stays Draft; limits 2/9 → null |
| REQ-03 stale SKU versus new price | PASS | B old price, A price 2,750,000, B SKU-only PATCH, both survive GET/SQL, stock unchanged |
| REQ-04 named stock position | PASS | 7 → 8, first 3, total 11; 400/409/409 refusals with zero change |
| REQ-05 wrong and lost response | PASS, connected-error branch | Wrong identity not success; lost response after real write reconciles by GET and does not auto-retry |
| REQ-06 cancel and 360x640 | PASS | Zero writes, reopen old SKU, overflow 0, controls present |
| REQ-07 no-change and independent sections | PASS | Zero-write no-change; 409 preserves second draft; later save persists both |

## Tool and cleanup evidence

- OpenSpec 2.1.1 strict: `Change 'seller-offer-safe-edit-283' is valid`, exit 0, recorded in `OPENSPEC-STRICT.log`.
- Playwright 1.65.1 / Chromium 1255, recorded in `TOOL-PLAYWRIGHT.txt`.
- Real Host 5090 and Next 3100 build `oWTA29FFSAnndeeVyDFsx` identities are in `EVIDENCE-MANIFEST.json` and 284 receipts.
- Disposable PostgreSQL `tooba_offer283_connected` was removed; 5090/3100 are closed; canonical 5088 `/health` 200 and 3000 `/fa` 200. The current read-only cleanup check found no disposable database row and no listeners. This is current-state evidence, not a rerun of the original cleanup.

## Independent review findings

- The harness genuinely uses served Next, real Host, a disposable PostgreSQL database, real seller headers and direct independent GET/SQL reads.
- The `REQ-04` foreign-target status in the 284 prose is wrong: the connected JSON records 409 `inventory.position.id_unknown`, not 400. The actual JSON is authoritative; all three negative branches are typed refusals with zero change. This is recorded in `EVIDENCE-REVIEW.md` and in the REQ-04 manifest limitation.
- No fresh connected rerun was performed. This is intentional: the original runtime is gone and this task is acceptance-only. The checker validates evidence bytes and facts; the review states the self-attestation limit.
- Cursor plan/review gate failures and their hashes remain intact. Cursor artifacts have no post-844 diff.
- No feature source changed after 611. The only changes are this task folder.

## Candidate method

`PROPOSED-OPEN-CODE-DELIVERY-METHOD.md` is prepared and marked **CANDIDATE — NOT ADOPTED**. It must not be merged into `AGENTS.md`, `docs/architecture/`, the pipeline or any other canonical governance until the architect and user explicitly approve a separate governance action.

## Next action

Architect review of this task folder, `VERIFY-OPEN-CODE.mjs`, `verification.log`, `EVIDENCE-MANIFEST.json` and `EVIDENCE-REVIEW.md`. If accepted, the architect may record the decision and decide whether to authorize a separate governance-only promotion. No next implementation task is created here.

STOP_FOR_ARCHITECT.
