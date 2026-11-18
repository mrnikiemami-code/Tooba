# TB-279-R2 — CLOSURE MATRIX

Each row names the issued requirement, the exact owning place that now enforces it, the fact that would have failed on
279-R1 code, and the honest limit. No row claims Architect or user acceptance.

## A — containment

| Requirement | Owner / source | Fact | Limit |
| --- | --- | --- | --- |
| Key XML, 20 scratch scripts and `RESULT.bridge.txt` out of Git | `22a0ec148` index removals + scoped `.gitignore` entries | `git show --stat 22a0ec148`; `git status --porcelain` empty afterwards | exposure remains in history and on disk |
| Local files preserved, active runtime key NOT deleted | private copy under `D:\temp\tb279r2-containment` (hash-compared, contents never printed) | `key-containment.md` | — |
| Active-key assessment and retirement procedure | `key-containment.md` | `Security-Exposure-State=CONTAINED_PENDING_REVIEWED_RETIREMENT` with impact (cookies/protected payloads) and exact next action | rotation needs Architect/user approval |

## B — connected wire contract (R1-A)

| Requirement | Owner / source | Fact | Limit |
| --- | --- | --- | --- |
| `start_refund` carries the reviewed decision revision (query only) | `return-api.ts:1517`, `ReturnAdminEndpoints` `expectedDecisionVersion`, `ReturnDirectory.cs:225/243/339` | mounted workspace fact "the correction sends … the exact expected decision version"; typed refusal on missing/stale | controlled ports, no live POST |
| reviewed `retry` carries failed id + decision revision | `return-api.ts:1407-1414`, `ReturnRetryIdentity` | 279 directory/Host facts for failed retry identity | same |
| resolution sends FIVE explicit portions | `return-api.ts:1530-1543`, `return-ui.tsx:259-260`, `ReturnAdminEndpoints:78` | mounted "the resolution dialog refuses an over-received split locally and sends one frozen body when lawful" | jsdom behaviour |
| custody carries `operator_attestation` + proof, condition-specific ceiling | `return-api.ts:1557-1565`, `ReturnDirectory.Resolutions.cs:312-327` | mounted "custody needs a real proof reference and stays inside the rejected remainder" | jsdom |
| snapshot mapper carries additive portion/version facts | `return-api.ts:534/575`, Order operations adapter `:85` | TypeScript compile + mounted facts | — |
| modal freezes the revisions of the snapshot it loaded | `return-ui.tsx:259-260` | mounted facts assert the sent values | — |

## C — portion capacity (R1-B)

| Requirement | Owner / source | Fact | Limit |
| --- | --- | --- | --- |
| held allocations read even with no active-status case | `ReturnEligibilityEvaluator.ReadOccupancyAsync` (`:185-210`) | rejected capacity facts (`A_rejected_case_re_acquires_its_own_released_capacity`, `A_rejected_capacity_that_a_sibling_case_already_took_is_a_typed_conflict`) | in-memory EF |
| occupied = accepted + owed custody on trim | `ReturnDirectory.Resolutions.cs:375-390`, `ReturnDirectory.Allocations.cs:100-129/136-190` | `R2_a_handed_back_remainder_keeps_the_accepted_units_occupied`: 3 delivered, 2 accepted, 1 rejected, 0.5 back ⇒ live allocation **2.5**, remaining **0.5** | in-memory EF over the real evaluator |
| rejected-only seller gains nothing | same occupancy read | rejected-capacity facts above | — |
| whole rejection with received goods keeps held amount | `ReleaseRejectedDeliveriesAsync` (`Allocations.cs:100-129`) | 279 policy suite facts | — |

## D — reviewed plan/version + unit quantum (R1-C/D)

| Requirement | Owner / source | Fact | Limit |
| --- | --- | --- | --- |
| approve/reject require reviewed decision version | `ReturnDirectory.cs:225/243/339`, `EnsureReviewedPlanVersion` (`Corrections.cs:237`) | `A_post_correction_decision_binds_the_current_revision_and_a_stale_one_cannot` | — |
| approval binds the reviewed resolution version | same gate + `ResolutionReviewVersionRequired` (`ReturnsErrorCodes.cs:559`, catalog `:98`, both resx) | `B_a_decision_over_a_reviewed_plan_must_carry_that_plan_version` (missing ⇒ `return.resolution.review_version_required`; stale ⇒ `return.correction.version_stale`; both zero rows) | — |
| atomic decision + frozen amount + capacity | `ReturnDirectory.cs:243` (`EnsureReviewedPlanVersion(lockedResolution, …)`) | `B_a_partial_plan_freezes_only_the_accepted_money_before_the_first_attempt` | controlled ports |
| portions align to the owning unit quantum | `ReturnDirectory.Resolutions.cs:104-149`, `:334-344` | `R2_a_plan_portion_finer_than_the_owning_unit_is_refused_before_any_row`, `R2_a_handback_finer_than_the_owning_unit_and_an_unproven_carrier_are_refused` | in-memory EF |
| exact stored occurrence replays before new gates | `ReturnDirectory.Resolutions.cs:252-302` | `C_custody_hands_back_only_the_rejected_remainder_and_never_claims_settlement` (exact replay) | — |

## E — evidenced custody and slice allocation (R1-D)

| Requirement | Owner / source | Fact | Limit |
| --- | --- | --- | --- |
| `carrier_delivered_back` refused as typed unavailable | `ReturnDirectory.Resolutions.cs:428-445` | `R2_a_handback_finer_than_the_owning_unit_and_an_unproven_carrier_are_refused` ⇒ `return.custody.proof_kind_unavailable`, 0 rows | no carrier reader exists |
| legacy ambiguity across ALL plausible slices (expired + live) | `ReturnAllocationPolicy.HasAmbiguousLegacy` (`:108`, used `:133`) | `An_old_legacy_case_is_never_guessed_onto_one_of_two_delivery_slices` | — |
| oldest-eligible first for NEW claims; expired never funds | `ReturnAllocationPolicy.Allocate` (`:120-155`) | `Two_deliveries_at_the_same_instant_merge_into_one_honest_slice`, evaluator facts | — |
| reopen re-acquires its exact previously owned slice | `EnsureDeliveryAllocationsAsync` / `TrimActiveAllocationsToHeldAsync` | `A_rejected_case_re_acquires_its_own_released_capacity` | in-memory EF |

## F — customer retained intent (R1-E)

| Requirement | Owner / source | Fact | Limit |
| --- | --- | --- | --- |
| unmount/close/retarget invalidates the epoch | `return-request-modal.tsx:187-201` | mounted "unmounting while the first of two children is on the wire stops the second send and never notifies" | jsdom |
| fence BEFORE every child send | same file `:374-410`, `:503`, `:578` | same fact + 279 submission facts | jsdom |
| legacy v1 acts recognised and blocking | `return-submission.ts:34-37/333-343` | mounted "an older viewer-less witness blocks a NEW act instead of pretending nothing is pending" | jsdom |
| child outcome compares the exact originating envelope | `return-request-modal.tsx:390-410` | mounted "replacing the frozen bytes while the answer is in flight never applies the old outcome" | jsdom |
| exact replay/unresolved retention preserved | `return-submission.ts` suite | FE unit "a child's answer is recorded against the exact act", "only the exact current act can be cleared" | — |

## G — connected proof scenarios

| Scenario | Covered by | Limit |
| --- | --- | --- |
| G1 start-refund freezes versions, query binding, duplicate replay, wrong/missing refuses | mounted 279 workspace facts + 279 directory/Host facts | controlled, no live POST |
| G2 reviewed failed retry with failed identity + version | directory/Host retry facts (`ReturnRetryIdentity`) | controlled |
| G3 resolution five portions → real Directory; accepted-only restock | `B_a_partial_plan…`, `R2_a_plan_portion…`, 279 policy facts | controlled ports |
| G4 whole/partial rejection + condition-specific handback, missing proof, carrier refusal, 2.5 occupancy | `C_custody_hands_back…`, `R2_a_handback…`, `R2_a_handed_back_remainder_keeps_the_accepted_units_occupied` | in-memory EF |
| G5 re-publish vs old approve, half-piece portion/custody, expired+live ambiguity, exact-slice reacquisition | `B_a_decision_over_a_reviewed_plan…`, `R2_a_plan_portion…`, `An_old_legacy_case_is_never_guessed…`, `A_rejected_case_re_acquires…` | — |
| G6 real customer modal lifetime/tamper/v1 | mounted 11/11 | jsdom |
| G7 existing rollback-only relational facts | Host 3/3 `committedRows=0` | real PostgreSQL, rollback-only |
| G8 strict transport assertions | FE unit 29/29 + mounted 34/34 + `tsc` 0 | — |
| G9 read-only 390/1440 DOM | served history checked at both widths, zero document overflow; active dialogs covered by mounted facts | no screenshot, no live case |
