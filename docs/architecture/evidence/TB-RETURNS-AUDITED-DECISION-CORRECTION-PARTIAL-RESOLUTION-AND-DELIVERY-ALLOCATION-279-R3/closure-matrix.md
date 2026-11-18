# TB-279-R3 — closure matrix (A1–A4 / B1–B4 / C1–C2 / D)

Every row cites a test that exists and was actually executed in this task (`ReturnsDecisionCorrection279DirectoryTests`
21/21, `ReturnsFinalClosure279R3Tests` 4/4). No row rests on a code-string/source-regex assertion.

| Gate | Requirement | Fact (actual test) | What it asserts |
| --- | --- | --- | --- |
| A1 | reopen → complete receipt, NO explicit plan → NEW approve without decision version refuses with zero writes | `R3_B1_a_reopened_case_refuses_an_unversioned_new_decision_then_accepts_the_reviewed_one` | `return.correction.version_stale`; decision count stays 1, revision count 1, status `Requested`, `ApprovedAmount` null, allocation count unchanged, gateway/inventory calls 0 |
| A2 | same for a NEW reject | `R3_B1_a_reopened_case_refuses_an_unversioned_new_rejection_before_any_row` | same typed refusal with no new decision/revision/status/allocation row, then the reviewed version succeeds |
| A3 | wrong/stale version refuses; exact current succeeds; genuine replay is zero-effect | same test (approve) and its reject counterpart | stale `0` ⇒ same typed refusal with no new revision; `1` ⇒ `Approved` + exactly one appended revision + frozen `2 × unit price`; replaying the SAME effective decision answers the stored fact and adds nothing |
| A4 | one reachable seller/Order adapter carries the frozen version or refuses its absence | `R3_B1_A4_the_reachable_admin_contract_never_invents_the_reviewed_revision` | the adapter is resolved from the SHIPPED `ReturnsModule` registration; `ApproveAsync` without a version ⇒ `return.correction.version_stale` with no revision; with the reviewed version ⇒ `Approved` and one appended revision |
| B1 | one delivered unit; legacy Rejected case received one, no custody, NO allocations ⇒ a second claim cannot take it | `R3_B2_a_legacy_rejected_case_with_received_goods_keeps_its_occupancy_without_rows` | remaining returnable `0`, slice availability `0`, `NothingReturnable`, no allocation rows were invented |
| B2 | same with ambiguous expired + live origins ⇒ typed blocked, not guessed free | `R3_B2_an_ambiguous_expired_and_live_origin_is_typed_blocked_not_guessed_free` | two slices (one out of window, one in window), every slice `LegacyAmbiguous`, `ReturnAllocationPolicy.Allocate(…, 1)` returns `null` |
| B3 | requested case with incoherent receiving + an existing live row: reject/release writes nothing | `R3_B2_an_incoherent_receipt_blocks_the_rejection_without_releasing_its_live_row` | `return.recovery.evidence_conflict`; decision 0, revision 0, status unchanged, live row still 1 and sum still `requested`, gateway/inventory 0 |
| B4 | positive handback releases only its own rejected quantity; mixed 2.5/0.5 retained | `R2_a_handed_back_remainder_keeps_the_accepted_units_occupied` | accepted 2 + owed 0.5 stays occupied (`2.5` live), remaining `0.5`, exactly two allocation rows (2.5 + released 0.5 split) |
| C1 | actual partial accepted restock through the real Directory | `C1_the_real_first_refund_restocks_exactly_the_accepted_portion_and_replays_with_zero_effect` | one durable attempt (`2 × unit price`, case-derived id/key, provider reference), gateway called once with the stored amount, recording Inventory receives **2** for the owning reservation, `UsableRestockedQuantity=2`, rejected-received `1` still owed, phase `PendingDisposition`, replay adds nothing |
| C2a | 60 + 60 over a 100 payment ⇒ typed budget refusal, no child/effect | `C2_the_real_failed_child_retry_refuses_a_cross_case_budget_overrun_and_mints_nothing` | `return.refund.budget_exceeded`, attempt count stays 1, gateway/Inventory empty, status still `RefundFailed` |
| C2b | lawful control creates exactly one deterministic child and replays it | `C2_an_independent_lawful_control_mints_exactly_one_deterministic_child_and_replays_it` | one child with the derived id/key and amount 60, one gateway call of 60, one accepted-usable restock of 1, duplicate reviewed intent mints nothing |
| C2c | missing payment authority refuses with zero attempt/effect | `C2_a_missing_payment_authority_refuses_the_retry_with_zero_attempt_and_effect` | `refund.payment_missing`, attempt count 1, gateway/Inventory empty |
| D | historical 277-R1 posting untracked index-only + exact scoped ignore | `git rm --cached` + `.gitignore` (`docs/architecture/evidence/**/RESULT.bridge.txt`) | local file preserved (`Test-Path` True), `git check-ignore` matches the exact path, historical `RESULT.md` untouched |

## Retained R2/R1 positives (not re-proved here beyond their own suites)

The operator/customer wire versions and portions, unit-quantum gates, custody per-condition ceilings, unproven-carrier
refusal, legacy slice ambiguity, 2.5 occupancy, customer epoch/v1/exact-origin retention, shared payment reservation and
the atomic decision transition are unchanged by this task and stay covered by their own retained facts
(`ReturnsDecisionCorrection279*`, `ReturnsFinalClosure279*`, mounted suites). This task neither weakened nor deleted them.

## Not claimed

- No Architect/user/provider/financial acceptance; parents remain PARTIAL.
- No real PSP/Wallet/Inventory effect; no committed two-writer race.
- No retirement of the historical key exposure (`CONTAINED_PENDING_REVIEWED_RETIREMENT`).
- No served operator walkthrough: there is no active case and no live POST is authorized.
