# Architect acceptance record — OpenCode pilot 283 / 284 / TB-OCC-284

Date: 2026-11-22
Decision: **ARCHITECT_ACCEPT — Stage 1 Seller Offer safe edit (issued scope only)**
Owner UI acceptance: **NOT_CLAIMED**
Permanent-method adoption: **APPROVED BY OWNER CONDITION (successful pilot) AND RECORDED**
Method: [TOOBA-OPENCODE-DELIVERY-V1@1.0.0](ARCHITECT-DELIVERY-METHOD.md)
Scope: Stage 1 only; Stage 2 not started or approved.

## Reviewed immutable artifacts

- Feature code commit: `611f97a85ec7c2e464e4f649820992202ca3754c`.
- Connected evidence commit: `fc40d412aa6d085d3db3f1deac461eb93a10f48d`.
- Independent OpenCode acceptance output: `78f654f5a96d0bf9c0660b0d39e93e5e80660ca3`.
- `docs/ai/tasks/TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284/evidence/connected-284.json` SHA256 `0a155bcc2fa5396b8d981e3246036523f70800a3bdf2e02db5c08fb89528afc1`.
- `docs/opencode/tasks/TB-OCC-284-OPEN-CODE-ACCEPTANCE-CLOSURE/EVIDENCE-REVIEW.md`, `EVIDENCE-MANIFEST.json`, `VERIFY-OPEN-CODE.mjs`, and `verification.log` read from GitHub.
- Seven REQs and 38 assertions, with negative self-tests: removed REQ, altered expected price and evidence tamper were all rejected.
- OpenSpec strict: recorded valid.
- Product source unchanged after feature commit.
- Historical Cursor gate FAIL remains intact and was not used as product acceptance.

## Findings / decision rationale

REQ-01 real seller SKU-only path persisted with exactly one Offer PATCH, no Pricing/Inventory write and preserved two stock locations 3/7, price, reservations and outbox. REQ-03 two real contexts preserved A's updated price while B saved stale-form SKU only; the independent GET and SQL agreed. REQ-02/04/05/06/07 also have primary connected evidence; intentional malformed/lost-response error branches used controlled substitution and were disclosed. No Stage-1 mandatory REQ remains open in the OpenCode evidence contract.

Review limitation: this architect inspected GitHub-persisted harness, evidence, manifest, validator design and negative-control records; the disposable database had already been removed. Therefore **this architect did not independently rerun the live connected harness** in this review, and logs are still worker-generated. Acceptance is bounded to verified artifact consistency and the previously executed connected test, not a newly executed infrastructure test. Owner acceptance remains separate.

Documentation discrepancy: one 284 RECEIPTS prose summary calls the foreign-stock error 400; primary JSON actually records 409 `inventory.position.id_unknown`; typed refusal and zero mutation satisfy the issued criterion. Do not rewrite the historical source.

## Separate governance authority

The owner required that a successful pilot be turned into an exact, unambiguous, reusable **architect** method, and explicitly confirmed its addition. Thus `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` is the approved OpenCode-only canonical procedure. This approval **does not** adopt/modify Cursor policy, edit AGENTS, update the Bridge, change domain architecture or authorize the next implementation task.

Next planned stage remains Seller Offer Stage 2, which requires a new separately issued authorized task after reviewing the current roadmap.

STOP_FOR_ARCHITECT.
