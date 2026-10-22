# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9 — RESULT

Status: **PASS — bounded historical-schema reconciliation analysis only** (no implementation, no database).
Whole TB-196 remains `BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE`.

| Item | Value |
| --- | --- |
| Mode | `BOUNDED_HISTORICAL_SCHEMA_RECONCILIATION_ANALYSIS_ONLY` |
| Expected base | `0e53f4603b299b2f5e61f6d4604ac9957a0c109f` |
| Actual base | `0e53f4603b299b2f5e61f6d4604ac9957a0c109f` (fast-forward from `8aeeaa385`, `HEAD == origin/main` before edits) |
| Claim-UTC | 2026-10-22T18:07:05Z |
| Result-UTC | 2026-10-22T18:16:36Z |
| Status of proposals | PROPOSED, NOT IMPLEMENTED, NOT APPROVED |

## 1. Reviewed baseline

Manifests were re-read from R6 evidence, not from prose:
`02-historical-legacy-manifest.txt` (192 objects, real PostgreSQL chain to `20261016120000_AddFulfillmentShippingGroups`),
`03-model-expected-legacy.txt` (216 objects), `04-historical-vs-model-raw-diff.txt`.
Recounted in this task: **16 `missing-in-actual` + 17 `unexpected-in-actual`** lines.

## 2. Derived discrepancy set (18)

| Class | Count | Objects |
| --- | --- | --- |
| Substantive — actually absent | 1 | `ix_outbox_messages_pending` on `fulfillment.outbox_messages` |
| Substantive — actually present only in DB | 2 | `ck_shipping_methods_code_canonical`, `ck_shipping_methods_option_code_canonical` |
| Defaults | 5 | `items.quantity_packed` (0), `items.quantity_processing` (0), `shipments.provider_metadata_version` (0), `shipments.shipping_method_code` (`''`), `shipments.shipping_method_label` (`''`) |
| Naming-only (full shape verified equal) | 10 | 1 FK (`fk_shipping_method_translations_method`) + 9 indexes |

Accounting: 5×2 + 10×2 + 3 = 33 = 16 + 17 — every manifest line is classified; no unclassified residue.
The R8-repaired 191 rename operations are **metadata-only** and are explicitly excluded from this set.
All 10 model-side names were re-verified verbatim in the post-R8 snapshot, so R8 did not invalidate the model-side facts.

**Unresolved facts (honest):** whether any live environment already has the outbox index out-of-band, and whether any write path bypasses the domain for `shipping_methods`, are `UNKNOWN_PENDING_EXECUTION` — no database was permitted, so no equality is claimed for those dimensions.

## 3. Recommended first slice (for Architect authorization only)

**Slice 1 — add `fulfillment.ix_outbox_messages_pending` in one Fulfillment migration.**
Additive, data-free, `CREATE INDEX IF NOT EXISTS … (occurred_at) WHERE processed_at IS NULL AND dead_lettered_at IS NULL`, `Down` = `DROP INDEX IF EXISTS`. No model/metadata/app change (the shared `OutboxMessageMapping` already declares it, and every sibling module creates it in its init migration — Fulfillment's `InitialFulfillment` simply omitted it).
Slices 2–4 (checks / defaults / names) are independent follow-ups, all proposing a **zero-DDL "model follows DB"** route; they are blocked on architect decisions D2–D4.

## 4. Decisions / blockers recorded

D1 outbox index mechanics (concurrent vs. transactional), D2 keep-vs-describe-vs-drop the two CHECKs,
D3 defaults (describe in model vs. keep model strict vs. drop defaults), D4 names (pin historical names vs. rename DB objects vs. declared naming equivalence), D5 which authority defines the production contract (database shape vs. model). Details in `RECONCILIATION-PROPOSAL.md §3`.

## 5. Checks actually run — SOURCE ANALYSIS ONLY

File reads, `rg` searches over repository sources and R6 manifests, and arithmetic recounting of the raw diff.
**Not run:** build, tests, `dotnet ef`, migrations, schema projection, PostgreSQL/container, server/Host, browser, HTTP, Bridge probe. No database engine was started; nothing was installed.

## 6. Changed files

* `docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9.task.md` (received artifact, verbatim)
* `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9/SOURCE-OF-TRUTH-MATRIX.md`
* `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9/RECONCILIATION-PROPOSAL.md`
* `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9/RESULT.md`
* `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md` (worker pointer paragraph only)

No `src/**`, manifest, certification, skill, Host, frontend or configuration file was touched. No approved decision text was rewritten; all proposals are marked unapproved.

## 7. Isolation / cleanup

No database, container, port, process, bin/obj or cache operation. No reset/stash/rebase/force-push; the 12 pre-existing untracked evidence helpers were left untouched. No temporary files were created outside the evidence directory.

## 8. Stop

Worker PASS covers completed bounded analysis only. `TB196-Acceptance: BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE`, `Production-Functional-Acceptance: PENDING`, `Stop-State: STOP_FOR_ARCHITECT`, `Automatic-Next-Task: NONE`.
