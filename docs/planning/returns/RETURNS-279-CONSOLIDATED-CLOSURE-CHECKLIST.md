# CURRENT — included in the ONE issued 279-R1 repair — 2026-11-18

Full envelope: `docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/TASK.md`. All F1–F6 and S1–S5 below are in scope; implementation/closure NOT yet claimed. No automatic next task. The original planning checklist is preserved below; its old “not issued” label describes its creation state.

# Returns — one consolidated closure checklist after TB-279
Date: 2026-11-18. Status: **CLOSED IN TB-279-R1 (worker PASS, awaiting Architect review)**.
Source checkpoint: 9c0885a5943e7f1a4e1a09a17fac417d23037aa6. Repair base: 0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32.
Read docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/ARCHITECT-REVIEW.md and, for the closure itself,
docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/closure-matrix.md.

User intent: finish real multi-seller/partial Returns quickly, then close small issues in the SAME bounded pass. Do not split this list into one task per helper, test or cosmetic issue. Do not forget small issues across chats. This checklist is not a worker envelope or implementation authorization.

| ID | Priority | Concrete closure evidence | State |
| --- | --- | --- | --- |
| F1 | blocking | `ReturnDirectory.Reservations.cs` is the ONE protocol for the first refund and every NEW retry child: owning payment facts before any lock, seller order → case → payment order, all Pending+Succeeded of the payment counted, commit before the external port; missing/foreign/non-Succeeded authority refuses. Fact: `ReturnsDecisionCorrection279Tests` + real-PG `ReturnsClosure279RelationalTests` (cross-case budget read + two-session lock contention) | **closed** |
| F2 | blocking | `ReturnResolutionPolicy` accepted-per-condition split; restock = accepted usable only; accepted 2-received + 1-unreceived progresses; mixed portions each need their own durable completion. Fact: `ReturnsDecisionCorrection279DirectoryTests` | **closed** |
| F3 | blocking | Rejected received goods keep their allocation occupancy until an evidenced handback; wrong-condition/over-handback/blank-proof/foreign-item refuse with zero rows; whole-case rejection exposes the same action. Fact: `ReturnsDecisionCorrection279Tests` | **closed** |
| F4 | blocking | `ExpectedDecisionVersion` travels to the command; stale refuses; decision+status+frozen amount+capacity release in ONE locked transaction; occurrence keys are SHA-256. Fact: 279 tests + real-PG stale-zero-write / lawful-atomic fact | **closed** |
| F5 | blocking | Legacy ambiguity publishes `return.allocation.legacy_ambiguous` with zero rows; new claims keep oldest-eligible ordering, expired-slice refusal, same-instant merge. Fact: `ReturnsDecisionCorrection279Tests` | **closed** |
| F6 | blocking | Viewer-scoped exact act, storage probe, byte re-verification, epoch fence, retained act never overwritten. Fact: `return-submission(-279).test.ts` + mounted modal suite | **closed** |
| S1 | small, same pass | Customer modal is the shared DS `Dialog`; single react-toastify container in AppProviders, one toast per command. Fact: mounted modal + workspace suites | **closed** |
| S2 | small, same pass | Unknown unit precision has one read/write policy this explicitly explains; lawful whole quantities preserved, no invented precision. Fact: `return-quantity`, mounted workspace facts | **closed** |
| S3 | small, same pass | Stale 276 mounted fixture updated to the current wire shape without weakening assertions. Fact: 276 mounted suite green inside the 44/44 returns bundle | **closed** |
| S4 | small, same pass | Human locator, own product/unit, requested vs approved money, accepted vs restocked and outstanding custody shown; no false "all refunded/settled". Fact: workspace detail + served chunk tokens | **closed** |
| S5 | small, same pass | Recovery banner, this plan and `docs/ai/TOOBA-RECOVERY-CONTEXT.md` point to the one current status with tests actually run and remaining limits; historical evidence preserved. Fact: `RECEIPTS.md` + `closure-matrix.md` | **closed** |

Verification actually run: Returns `172/172`; AMSC guards `34/34`; real-PG rollback-only `3/3` with `committedRows=0`;
Host build/publish 0 errors; FE `44/44`, `tsc` 0, `next build` 0. One additive Returns migration
(`20261118023434_Tb279R1PerConditionPortionsAndCustodyProof`) after a verified fresh backup.

Current acceptance: 277/R1/R2 bounded acceptances retained; 278 PARTIAL; 279 PARTIAL until this repair is reviewed.
Worker PASS is not Architect/user/provider/financial acceptance. No next task issued. automaticNextImplementationTask: NONE.
