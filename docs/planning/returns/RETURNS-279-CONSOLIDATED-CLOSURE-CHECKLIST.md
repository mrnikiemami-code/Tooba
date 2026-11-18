# CURRENT — included in the ONE issued 279-R1 repair — 2026-11-18

Full envelope: `docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/TASK.md`. All F1–F6 and S1–S5 below are in scope; implementation/closure NOT yet claimed. No automatic next task. The original planning checklist is preserved below; its old “not issued” label describes its creation state.

# Returns — one consolidated closure checklist after TB-279
Date: 2026-11-18. Status: PLANNED, NOT ISSUED.
Source checkpoint: 9c0885a5943e7f1a4e1a09a17fac417d23037aa6.
Read docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/ARCHITECT-REVIEW.md.

User intent: finish real multi-seller/partial Returns quickly, then close small issues in the SAME bounded pass. Do not split this list into one task per helper, test or cosmetic issue. Do not forget small issues across chats. This checklist is not a worker envelope or implementation authorization.

| ID | Priority | Concrete closure evidence | State |
| --- | --- | --- | --- |
| F1 | blocking | First and failed-child retry share atomic payment reservation; missing payment authority refuses; no external port under transaction; A-failed/B-reserved/A-retry refuses without new attempt | open |
| F2 | blocking | Received 3 usable / accepted 2 / rejected 1 calls Inventory for 2 only; accepted 2 received + rejected 1 unreceived progresses; mixed condition portions carry their own durable completion | open |
| F3 | blocking | Reject received goods retains their claimed capacity until evidenced actual handback; wrong-condition handback refuses; whole-case rejection offers lawful custody action | open |
| F4 | blocking | Frozen reviewed versions travel from modal to command; stale version cannot start money after reopen/reapprove; decision+status+amount/allocation atomic; exact stable occurrence replay | open |
| F5 | blocking | Ambiguous multi-delivery legacy claim blocks without guessing; new claims retain oldest-eligible ordering and own deadlines | open |
| F6 | blocking | Pending/unreadable customer act cannot be overwritten; viewer switch/retarget/unmount fences later sends/UI; body tampering refuses; exact compare/update/clear | open |
| S1 | small, same pass | Customer modal uses shared Dialog: focus return, Escape/cancel, local scrolling at 390/1440, no document overflow; existing template toast once per command | open |
| S2 | small, same pass | Unknown unit-precision read/write eligibility is consistent and explicitly explained; no assumed fraction precision, no unit-specific hardcode | open |
| S3 | small, same pass | Retained 276 mounted fixture is updated to accepted current wire shape without weakening assertions; truly obsolete coverage explicitly superseded | open |
| S4 | small, same pass | Human locator/product/unit/accepted amount versus requested amount/remaining custody clearly shown; no false “all refunded/settled” or permanent technical pending copy | open |
| S5 | small, same pass | Recovery banner, plan and START-HERE point to one current status; report tests actually run and remaining limits, preserve historical evidence | open |

Suggested verification scope when a consolidated repair is explicitly issued: regression facts for F1–F6 through the actual owning Directory and mounted customer/operator components; narrowly scoped rollback-only relational facts for the shared reservation/decision coordination, with canonical target verification and zero committed rows. No broad test loops, source-regex substitute, real PSP/Wallet/Inventory action, committed fixture, second database, screenshot, or reset of the three completed cases. Any necessary schema change requires an explicit reviewed envelope first; existing applied migrations must never be edited or reversed.

Current acceptance: 277/R1/R2 bounded acceptances retained; 278 PARTIAL; 279 PARTIAL. No customer/Marketplace/provider/financial readiness. No next task issued. automaticNextImplementationTask: NONE.
